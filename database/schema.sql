-- Legal Document Management System - Relational Schema
-- Generated for deployment on PostgreSQL 14+

BEGIN;

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =============================================================================
-- 1. Users
-- =============================================================================
CREATE TABLE IF NOT EXISTS users (
    user_id        SERIAL PRIMARY KEY,
    username       VARCHAR(50)  NOT NULL UNIQUE,
    password_hash  VARCHAR(255) NOT NULL,
    full_name      VARCHAR(150) NOT NULL,
    email          VARCHAR(150),
    role           VARCHAR(30)  NOT NULL,
    is_active      BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at     TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    last_login     TIMESTAMPTZ,
    CONSTRAINT chk_users_role CHECK (
        role IN (
            'admin',
            'senior_lawyer',
            'junior_lawyer',
            'legal_researcher',
            'archiving',
            'accountant',
            'read_only'
        )
    )
);

CREATE INDEX IF NOT EXISTS idx_users_role ON users(role);

-- =============================================================================
-- 2. Folders
-- =============================================================================
CREATE TABLE IF NOT EXISTS folders (
    folder_id   SERIAL PRIMARY KEY,
    parent_id   INTEGER REFERENCES folders(folder_id) ON DELETE CASCADE,
    folder_name VARCHAR(150) NOT NULL,
    folder_path TEXT         NOT NULL,
    created_by  INTEGER REFERENCES users(user_id) ON DELETE SET NULL,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    UNIQUE(folder_path)
);

CREATE INDEX IF NOT EXISTS idx_folders_parent ON folders(parent_id);
CREATE INDEX IF NOT EXISTS idx_folders_created_by ON folders(created_by);

-- =============================================================================
-- 3. Documents
-- =============================================================================
CREATE TABLE IF NOT EXISTS documents (
    document_id     SERIAL PRIMARY KEY,
    folder_id       INTEGER REFERENCES folders(folder_id) ON DELETE SET NULL,
    document_name   VARCHAR(255) NOT NULL,
    document_type   VARCHAR(50),
    file_guid       UUID         NOT NULL DEFAULT gen_random_uuid(),
    file_path       TEXT         NOT NULL,
    file_size       BIGINT,
    mime_type       VARCHAR(100),
    current_version INTEGER      NOT NULL DEFAULT 1,
    is_locked       BOOLEAN      NOT NULL DEFAULT FALSE,
    locked_by       INTEGER REFERENCES users(user_id) ON DELETE SET NULL,
    locked_at       TIMESTAMPTZ,
    uploaded_by     INTEGER REFERENCES users(user_id) ON DELETE SET NULL,
    uploaded_at     TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    tags            TEXT[]       NOT NULL DEFAULT ARRAY[]::TEXT[],
    metadata        JSONB        NOT NULL DEFAULT '{}'::JSONB,
    search_vector   TSVECTOR,
    CONSTRAINT uq_documents_file_guid UNIQUE(file_guid)
);

CREATE INDEX IF NOT EXISTS idx_documents_folder ON documents(folder_id);
CREATE INDEX IF NOT EXISTS idx_documents_uploaded_by ON documents(uploaded_by);
CREATE INDEX IF NOT EXISTS idx_documents_locked_by ON documents(locked_by);
CREATE INDEX IF NOT EXISTS idx_documents_tags ON documents USING GIN (tags);
CREATE INDEX IF NOT EXISTS idx_documents_search_vector ON documents USING GIN (search_vector);

-- =============================================================================
-- 4. Document Versions
-- =============================================================================
CREATE TABLE IF NOT EXISTS document_versions (
    version_id        SERIAL PRIMARY KEY,
    document_id       INTEGER NOT NULL REFERENCES documents(document_id) ON DELETE CASCADE,
    version_number    INTEGER NOT NULL,
    file_path         TEXT    NOT NULL,
    file_size         BIGINT,
    is_final          BOOLEAN NOT NULL DEFAULT FALSE,
    change_description TEXT,
    created_by        INTEGER REFERENCES users(user_id) ON DELETE SET NULL,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(document_id, version_number)
);

CREATE INDEX IF NOT EXISTS idx_doc_versions_document ON document_versions(document_id);
CREATE INDEX IF NOT EXISTS idx_doc_versions_created_by ON document_versions(created_by);

-- =============================================================================
-- 5. Audit Log
-- =============================================================================
CREATE TABLE IF NOT EXISTS audit_log (
    log_id      SERIAL PRIMARY KEY,
    user_id     INTEGER REFERENCES users(user_id) ON DELETE SET NULL,
    action      VARCHAR(50) NOT NULL,
    entity_type VARCHAR(30) NOT NULL,
    entity_id   INTEGER,
    details     TEXT,
    ip_address  VARCHAR(45),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_audit_user ON audit_log(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_created_at ON audit_log(created_at);
CREATE INDEX IF NOT EXISTS idx_audit_entity ON audit_log(entity_type, entity_id);

-- =============================================================================
-- 6. Settings
-- =============================================================================
CREATE TABLE IF NOT EXISTS settings (
    setting_key   VARCHAR(100) PRIMARY KEY,
    setting_value TEXT,
    description   TEXT,
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- 7. Outgoing Correspondence
-- =============================================================================
CREATE TABLE IF NOT EXISTS outgoing (
    outgoing_id      SERIAL PRIMARY KEY,
    document_id      INTEGER REFERENCES documents(document_id) ON DELETE SET NULL,
    outgoing_number  VARCHAR(60) NOT NULL UNIQUE,
    recipient_name   VARCHAR(200) NOT NULL,
    recipient_address TEXT,
    subject          VARCHAR(500) NOT NULL,
    send_date        DATE        NOT NULL DEFAULT CURRENT_DATE,
    delivery_method  VARCHAR(50),
    tracking_number  VARCHAR(120),
    notes            TEXT,
    created_by       INTEGER REFERENCES users(user_id) ON DELETE SET NULL,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_outgoing_send_date ON outgoing(send_date);
CREATE INDEX IF NOT EXISTS idx_outgoing_recipient ON outgoing(recipient_name);

-- =============================================================================
-- 8. Incoming Correspondence
-- =============================================================================
CREATE TABLE IF NOT EXISTS incoming (
    incoming_id      SERIAL PRIMARY KEY,
    document_id      INTEGER REFERENCES documents(document_id) ON DELETE SET NULL,
    incoming_number  VARCHAR(60) NOT NULL UNIQUE,
    sender_name      VARCHAR(200) NOT NULL,
    sender_address   TEXT,
    subject          VARCHAR(500) NOT NULL,
    received_date    DATE        NOT NULL DEFAULT CURRENT_DATE,
    original_number  VARCHAR(120),
    priority         VARCHAR(20) NOT NULL DEFAULT 'normal',
    requires_response BOOLEAN    NOT NULL DEFAULT FALSE,
    response_deadline DATE,
    notes            TEXT,
    created_by       INTEGER REFERENCES users(user_id) ON DELETE SET NULL,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_incoming_priority CHECK (priority IN ('normal','urgent','confidential'))
);

CREATE INDEX IF NOT EXISTS idx_incoming_received_date ON incoming(received_date);
CREATE INDEX IF NOT EXISTS idx_incoming_sender ON incoming(sender_name);
CREATE INDEX IF NOT EXISTS idx_incoming_priority ON incoming(priority);

-- =============================================================================
-- 9. Tasks
-- =============================================================================
CREATE TABLE IF NOT EXISTS tasks (
    task_id         SERIAL PRIMARY KEY,
    task_title      VARCHAR(200) NOT NULL,
    task_description TEXT,
    assigned_to     INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    assigned_by     INTEGER NOT NULL REFERENCES users(user_id) ON DELETE SET NULL,
    document_id     INTEGER REFERENCES documents(document_id) ON DELETE SET NULL,
    priority        VARCHAR(20) NOT NULL DEFAULT 'normal',
    status          VARCHAR(20) NOT NULL DEFAULT 'pending',
    due_date        DATE,
    completed_at    TIMESTAMPTZ,
    notes           TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_tasks_priority CHECK (priority IN ('low','normal','high','critical')),
    CONSTRAINT chk_tasks_status CHECK (status IN ('pending','in_progress','completed','cancelled'))
);

CREATE INDEX IF NOT EXISTS idx_tasks_assigned_to ON tasks(assigned_to);
CREATE INDEX IF NOT EXISTS idx_tasks_status ON tasks(status);
CREATE INDEX IF NOT EXISTS idx_tasks_due_date ON tasks(due_date);

-- =============================================================================
-- 10. Task Comments
-- =============================================================================
CREATE TABLE IF NOT EXISTS task_comments (
    comment_id   SERIAL PRIMARY KEY,
    task_id      INTEGER NOT NULL REFERENCES tasks(task_id) ON DELETE CASCADE,
    user_id      INTEGER REFERENCES users(user_id) ON DELETE SET NULL,
    comment_text TEXT      NOT NULL,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_task_comments_task ON task_comments(task_id);
CREATE INDEX IF NOT EXISTS idx_task_comments_user ON task_comments(user_id);

-- =============================================================================
-- 11. OCR Queue
-- =============================================================================
CREATE TABLE IF NOT EXISTS ocr_queue (
    queue_id      SERIAL PRIMARY KEY,
    document_id   INTEGER NOT NULL REFERENCES documents(document_id) ON DELETE CASCADE,
    status        VARCHAR(20) NOT NULL DEFAULT 'pending',
    error_message TEXT,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at  TIMESTAMPTZ,
    CONSTRAINT chk_ocr_status CHECK (status IN ('pending','processing','completed','failed'))
);

CREATE INDEX IF NOT EXISTS idx_ocr_queue_status ON ocr_queue(status);
CREATE INDEX IF NOT EXISTS idx_ocr_queue_document ON ocr_queue(document_id);

-- =============================================================================
-- 12. Shared Links
-- =============================================================================
CREATE TABLE IF NOT EXISTS shared_links (
    link_id              SERIAL PRIMARY KEY,
    document_id          INTEGER NOT NULL REFERENCES documents(document_id) ON DELETE CASCADE,
    link_token           VARCHAR(100) NOT NULL UNIQUE,
    created_by           INTEGER REFERENCES users(user_id) ON DELETE SET NULL,
    expires_at           TIMESTAMPTZ NOT NULL,
    max_access_count     INTEGER,
    current_access_count INTEGER NOT NULL DEFAULT 0,
    password_hash        VARCHAR(255),
    is_active            BOOLEAN NOT NULL DEFAULT TRUE,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_shared_links_document ON shared_links(document_id);
CREATE INDEX IF NOT EXISTS idx_shared_links_active ON shared_links(is_active, expires_at);

-- =============================================================================
-- 13. Email Log
-- =============================================================================
CREATE TABLE IF NOT EXISTS email_log (
    email_id     SERIAL PRIMARY KEY,
    document_id  INTEGER REFERENCES documents(document_id) ON DELETE SET NULL,
    sent_to      VARCHAR(200) NOT NULL,
    subject      VARCHAR(500),
    body         TEXT,
    sent_by      INTEGER REFERENCES users(user_id) ON DELETE SET NULL,
    sent_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    status       VARCHAR(20) NOT NULL DEFAULT 'sent',
    error_message TEXT,
    CONSTRAINT chk_email_status CHECK (status IN ('sent','failed','queued'))
);

CREATE INDEX IF NOT EXISTS idx_email_log_document ON email_log(document_id);
CREATE INDEX IF NOT EXISTS idx_email_log_sent_by ON email_log(sent_by);
CREATE INDEX IF NOT EXISTS idx_email_log_sent_at ON email_log(sent_at);

-- =============================================================================
-- 14. Link Access Log
-- =============================================================================
CREATE TABLE IF NOT EXISTS link_access_log (
    access_id   SERIAL PRIMARY KEY,
    link_id     INTEGER NOT NULL REFERENCES shared_links(link_id) ON DELETE CASCADE,
    accessed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ip_address  VARCHAR(45),
    user_agent  TEXT
);

CREATE INDEX IF NOT EXISTS idx_link_access_log_link ON link_access_log(link_id);
CREATE INDEX IF NOT EXISTS idx_link_access_log_accessed_at ON link_access_log(accessed_at);

-- =============================================================================
-- Full-text search trigger
-- =============================================================================
CREATE OR REPLACE FUNCTION documents_search_vector_update()
RETURNS TRIGGER AS $$
BEGIN
    NEW.search_vector :=
        setweight(to_tsvector('arabic', COALESCE(NEW.document_name, '')), 'A') ||
        setweight(to_tsvector('arabic', COALESCE(NEW.document_type, '')), 'B') ||
        setweight(to_tsvector('arabic', COALESCE(array_to_string(NEW.tags, ' '), '')), 'C') ||
        setweight(to_tsvector('arabic', COALESCE(NEW.metadata->>'ocr_text', '')), 'D');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_documents_search_vector ON documents;
CREATE TRIGGER trg_documents_search_vector
BEFORE INSERT OR UPDATE ON documents
FOR EACH ROW
EXECUTE FUNCTION documents_search_vector_update();

COMMIT;

