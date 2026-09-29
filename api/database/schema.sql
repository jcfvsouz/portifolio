-- Campaign Publication & Batch Email Dispatch - schema
-- Run once against the target database (see ../../README.md - the docker/ SQL Server
-- container isn't wired up yet, so this runs manually until then).

CREATE TABLE Tenants (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL
);

CREATE TABLE BuyerGroups (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL REFERENCES Tenants (Id),
    Name NVARCHAR(200) NOT NULL
);

CREATE TABLE Buyers (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    BuyerGroupId UNIQUEIDENTIFIER NOT NULL REFERENCES BuyerGroups (Id),
    Name NVARCHAR(200) NOT NULL,
    Email NVARCHAR(320) NOT NULL
);

-- Status: 0 = Draft, 1 = Published - matches Promo.Api.Domain.CampaignStatus's underlying
-- int values; Dapper reads/writes the enum as this same int with no extra mapping.
CREATE TABLE Campaigns (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL REFERENCES Tenants (Id),
    BuyerGroupId UNIQUEIDENTIFIER NOT NULL REFERENCES BuyerGroups (Id),
    Name NVARCHAR(200) NOT NULL,
    Status INT NOT NULL DEFAULT 0,
    PublishedAtUtc DATETIME2 NULL
);
