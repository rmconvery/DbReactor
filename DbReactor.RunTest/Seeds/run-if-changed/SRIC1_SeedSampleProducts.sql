-- Seed script that runs if the content changes
-- Uses folder structure to determine strategy (run-if-changed)

-- Clear existing sample products first
DELETE FROM Products WHERE ProductName LIKE 'Sample%'

-- Insert sample products for ${Environment} environment
INSERT INTO Products (ProductName, Price, CreatedAt)
VALUES 
    ('Sample Widget', 19.98, GETUTCDATE()),
    ('Sample Gadget', 29.99, GETUTCDATE()),
    ('Sample Tool', 39.99, GETUTCDATE())

-- Add environment-specific products
IF '${Environment}' = 'Development'
BEGIN
    INSERT INTO Products (ProductName, Price, CreatedAt)
    VALUES ('Dev Test Product', 0.01, GETUTCDATE())
END

PRINT 'Sample products seeded for ' + '${Environment}' + ' environment'