-- Seed script that runs only once to create an admin user
-- Uses 'runonce' naming convention to determine strategy

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, Email, CreatedAt)
    VALUES ('admin', 'admin@${CompanyDomain}', GETUTCDATE())
    
    PRINT 'Admin user created successfully'
END
ELSE
BEGIN
    PRINT 'Admin user already exists, skipping creation'
END