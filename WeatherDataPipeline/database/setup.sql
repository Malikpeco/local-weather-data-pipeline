IF DB_ID('WeatherPipelineDb') IS NULL
BEGIN
    CREATE DATABASE WeatherPipelineDb;
END;
GO

USE WeatherPipelineDb;
GO

IF OBJECT_ID('dbo.WeatherReadings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WeatherReadings
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        City NVARCHAR(100) NOT NULL,
        TemperatureCelsius FLOAT NOT NULL,
        HumidityPercent INT NOT NULL,
        WindSpeedKmh FLOAT NOT NULL,
        WeatherCode INT NOT NULL,
        RecordedAtUtc DATETIME NOT NULL
    );
END;
GO