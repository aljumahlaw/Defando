-- =============================================================================
-- Migration: Add index on documents.uploaded_at for improved query performance
-- =============================================================================
-- Description: Most queries sort by uploaded_at DESC, so an index will significantly
--              improve performance, especially with large datasets.
-- Date: 2025-01-XX
-- =============================================================================

BEGIN;

-- Create index on uploaded_at column (DESC order for most common query pattern)
CREATE INDEX IF NOT EXISTS idx_documents_uploaded_at ON documents(uploaded_at DESC);

COMMIT;




