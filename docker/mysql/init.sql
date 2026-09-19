-- Mooncake E-Commerce Platform — MySQL Initialization Script
-- Charset & collation
ALTER DATABASE mooncake_db CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- Force UTC timezone for all connections
SET GLOBAL time_zone = '+00:00';
SET time_zone = '+00:00';
