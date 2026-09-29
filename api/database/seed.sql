-- Campaign Publication & Batch Email Dispatch - seed data
-- Fixed GUIDs on purpose: the README/samples reference these BuyerGroupIds directly when
-- demonstrating POST /campaigns, so a fresh database always has the same two groups to target.

INSERT INTO Tenants (Id, Name)
VALUES ('11111111-1111-1111-1111-111111111111', 'Acme Retail');

INSERT INTO BuyerGroups (Id, TenantId, Name)
VALUES
    ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'VIP Customers'),
    ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', 'Newsletter Subscribers');

INSERT INTO Buyers (Id, BuyerGroupId, Name, Email)
VALUES
    (NEWID(), '22222222-2222-2222-2222-222222222222', 'Ana Souza', 'ana.souza@example.com'),
    (NEWID(), '22222222-2222-2222-2222-222222222222', 'Bruno Lima', 'bruno.lima@example.com'),
    (NEWID(), '22222222-2222-2222-2222-222222222222', 'Carla Mendes', 'carla.mendes@example.com'),
    (NEWID(), '33333333-3333-3333-3333-333333333333', 'Diego Alves', 'diego.alves@example.com'),
    (NEWID(), '33333333-3333-3333-3333-333333333333', 'Elisa Rocha', 'elisa.rocha@example.com');
