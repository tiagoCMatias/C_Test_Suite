start %cd%mdb_test.exe %1 %2
timeout /t 20
taskkill /im mdb_test.exe