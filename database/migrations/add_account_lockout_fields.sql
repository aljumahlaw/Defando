-- Migration: Add Account Lockout fields to users table
-- Date: 2025
-- Description: Adds fields to support account lockout after failed login attempts

BEGIN;

-- Add failed_login_attempts column
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS failed_login_attempts INTEGER NOT NULL DEFAULT 0;

-- Add locked_until column
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS locked_until TIMESTAMPTZ;

-- Add index on locked_until for efficient queries
CREATE INDEX IF NOT EXISTS idx_users_locked_until ON users(locked_until) 
WHERE locked_until IS NOT NULL;

COMMENT ON COLUMN users.failed_login_attempts IS 'Number of consecutive failed login attempts';
COMMENT ON COLUMN users.locked_until IS 'Timestamp when the account lockout expires';

COMMIT;

