-- Maintenance script that runs every time
-- Uses folder structure to determine strategy (run-always)

-- Update table statistics
UPDATE STATISTICS Users
UPDATE STATISTICS Products  
UPDATE STATISTICS Orders

-- Log the maintenance run
PRINT 'Database statistics updated at ' + CONVERT(VARCHAR, GETUTCDATE(), 120) + ' for ${Environment}'

-- Environment-specific maintenance
IF '${Environment}' IN ('Production', 'Staging')
BEGIN
    -- Additional maintenance for production environments
    PRINT 'Production maintenance completed'
END