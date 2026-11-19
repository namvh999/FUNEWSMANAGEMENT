@echo off
chcp 65001 >nul
echo =========================================================================
echo   KHOI CHAY HE THONG PHAN TAN FUNewsManagementSystem (PRN232 Assignment 2)
echo =========================================================================
echo.
echo [1/4] Dang khoi dong Core API (Port: 5001)...
start "FUNews - Core API (5001)" cmd /k "cd FUNewsManagement_CoreAPI && dotnet run --launch-profile http"

timeout /t 3 /nobreak >nul

echo [2/4] Dang khoi dong Analytics API (Port: 5002)...
start "FUNews - Analytics API (5002)" cmd /k "cd FUNewsManagement_AnalyticsAPI && dotnet run --launch-profile http"

timeout /t 2 /nobreak >nul

echo [3/4] Dang khoi dong AI API (Port: 5003)...
start "FUNews - AI API (5003)" cmd /k "cd FUNewsManagement_AIAPI && dotnet run --launch-profile http"

timeout /t 2 /nobreak >nul

echo [4/4] Dang khoi dong Frontend MVC (Port: 5000)...
start "FUNews - Frontend (5000)" cmd /k "cd FUNewsManagement_FE && dotnet run --launch-profile http"

echo.
echo =========================================================================
echo   HE THONG DA KHOI CHAY THANH CONG!
echo   - Frontend:      http://localhost:5000
echo   - Core API:      http://localhost:5001/swagger
echo   - Analytics API: http://localhost:5002/swagger
echo   - AI API:        http://localhost:5003/swagger
echo =========================================================================
pause
