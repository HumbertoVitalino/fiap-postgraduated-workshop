-- Dados de demonstração pra quem sobe o docker-compose pra avaliar o projeto (ex.: professor/corretor)
-- e quer ver o sistema já populado, sem precisar criar tudo manualmente antes de explorar a API.
--
-- Só roda no ambiente docker-compose (serviço sqlserver-seed-demo, depois do sqlserver-init) — não é
-- aplicado pelos testes de integração, que continuam usando só db/init.sql via DatabaseFixture. Mantido
-- num arquivo separado de propósito, pra não misturar dado de demonstração com o schema/seed mínimo
-- (usuário Admin de bootstrap) que os dois ambientes compartilham.
--
-- Idempotente (guardado por IF NOT EXISTS no e-mail da atendente): rodar mais de uma vez não duplica.
-- Cobre um cenário completo de oficina — clientes, veículos, catálogo de serviços/peças com estoque —
-- e um histórico de Ordens de Serviço passando por cada status da máquina de estados (Recebida,
-- Em diagnóstico, Aguardando aprovação, Em execução, Finalizada, Cancelada, Entregue), incluindo os dois
-- desfechos de Entregue (via Finalizada e via Cancelada). Os valores calculados (Subtotal/Total de cada
-- orçamento, reserva/baixa de estoque, média incremental de EstimatedDuration/ExecutionCount dos
-- serviços) foram computados à mão seguindo exatamente as mesmas fórmulas do domínio (ver
-- Service.RecordExecution / InventoryItem.Reserve|CommitReservation em CONTEXT.md §3), na ordem
-- cronológica real dos eventos (mais antigo primeiro), pra ficarem consistentes entre si.

USE $(DatabaseName);
GO

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'atendente@oficina.com')
BEGIN
    DECLARE @Now DATETIME2 = SYSDATETIME();

    ------------------------------------------------------------------
    -- Usuários (além do Admin de bootstrap já semeado por db/init.sql)
    -- Senha 'Demo@123' pros dois, hash BCrypt (work factor 12) pré-computado
    -- do mesmo jeito que o Admin — ver comentário em db/init.sql.
    ------------------------------------------------------------------
    DECLARE @UserAttendant UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111001';
    DECLARE @UserMechanic  UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111002';

    INSERT INTO Users (Id, Email, Name, Password, Role, CreatedAt, UpdatedAt)
    VALUES
        (@UserAttendant, 'atendente@oficina.com', 'Ana Ferreira', '$2a$12$X1FrQMMiwazckLCU3m4l4.IdIG4yn0VrVPGsBt7UYgzpFbwwf1Mw.', 'Attendant', DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now)),
        (@UserMechanic,  'mecanico@oficina.com', 'Carlos Souza', '$2a$12$X1FrQMMiwazckLCU3m4l4.IdIG4yn0VrVPGsBt7UYgzpFbwwf1Mw.', 'Mechanic',  DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now));

    ------------------------------------------------------------------
    -- Clientes (CPF/CNPJ com dígito verificador válido, calculado pelo
    -- mesmo algoritmo que TestData.Document() usa nos testes de integração)
    ------------------------------------------------------------------
    DECLARE @CustomerMaria    UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222220001';
    DECLARE @CustomerJoao     UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222220002';
    DECLARE @CustomerEstrela  UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222220003';
    DECLARE @CustomerFernanda UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222220004';

    INSERT INTO Customers (Id, Name, Document, Email, Phone, CreatedAt, UpdatedAt)
    VALUES
        (@CustomerMaria,    'Maria Oliveira Santos',      '12345678909',   'maria.santos@example.com',        '11987654321', DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now)),
        (@CustomerJoao,     'João Pedro Almeida',         '98765432100',   'joao.almeida@example.com',        '11987654322', DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now)),
        (@CustomerEstrela,  'Auto Peças Estrela Ltda',    '11223344000186','contato@autopecasestrela.com.br', '1130304050',  DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now)),
        (@CustomerFernanda, 'Fernanda Costa Lima',        '32145678964',   'fernanda.lima@example.com',       '11987654323', DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now));

    ------------------------------------------------------------------
    -- Veículos
    ------------------------------------------------------------------
    DECLARE @VehicleCivic    UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333330001';
    DECLARE @VehicleArgo     UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333330002';
    DECLARE @VehicleSaveiro  UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333330003';
    DECLARE @VehicleCorolla  UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333330004';
    DECLARE @VehicleRenegade UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333330005';

    INSERT INTO Vehicles (Id, CustomerId, LicensePlate, Brand, Model, ManufactureYear, ModelYear, Color, CreatedAt, UpdatedAt)
    VALUES
        (@VehicleCivic,    @CustomerMaria,    'ABC1234', 'Honda',  'Civic',    2020, 2021, 'Prata',    DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now)),
        (@VehicleArgo,     @CustomerJoao,     'OIT2C34', 'Fiat',   'Argo',     2019, 2019, 'Branco',   DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now)),
        (@VehicleSaveiro,  @CustomerEstrela,  'RJK9876', 'Volkswagen', 'Saveiro', 2018, 2018, 'Prata', DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now)),
        (@VehicleCorolla,  @CustomerFernanda, 'PSA4B21', 'Toyota', 'Corolla',  2022, 2022, 'Preto',    DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now)),
        (@VehicleRenegade, @CustomerMaria,    'MEC5678', 'Jeep',   'Renegade', 2021, 2022, 'Vermelho', DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now));

    ------------------------------------------------------------------
    -- Catálogo de serviços. EstimatedDuration em minutos (convenção implícita
    -- do projeto, ver CONTEXT.md §3). S1/S2 têm ExecutionCount/EstimatedDuration
    -- já refletindo a média incremental depois da OS5 (Finalizada) mais abaixo —
    -- ver o valor final calculado nos comentários de cada linha.
    ------------------------------------------------------------------
    DECLARE @ServiceOilChange UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444440001';
    DECLARE @ServiceAlignment UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444440002';
    DECLARE @ServiceFullReview UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444440003';
    DECLARE @ServiceBrakePads UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444440004';
    DECLARE @ServiceTimingBelt UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444440005';
    DECLARE @ServiceDiagnostic UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444440006';

    INSERT INTO Services (Id, Code, Name, Description, BasePrice, EstimatedDuration, ExecutionCount, IsActive, CreatedAt, UpdatedAt)
    VALUES
        -- EstimatedDuration=45/ExecutionCount=1: valor original era 40min, atualizado pela conclusão da OS5 (ActualDuration=45)
        (@ServiceOilChange,  'SVC-001', 'Troca de óleo e filtro',         'Troca de óleo do motor e filtro de óleo',                     150.00, 45, 1, 1, DATEADD(DAY, -90, @Now), DATEADD(DAY, -10, @Now)),
        -- EstimatedDuration=55/ExecutionCount=1: valor original era 60min, atualizado pela conclusão da OS5 (ActualDuration=55)
        (@ServiceAlignment,  'SVC-002', 'Alinhamento e balanceamento',    'Alinhamento de direção e balanceamento das quatro rodas',    120.00, 55, 1, 1, DATEADD(DAY, -90, @Now), DATEADD(DAY, -10, @Now)),
        (@ServiceFullReview, 'SVC-003', 'Revisão completa',               'Revisão geral com checklist de 40 itens',                     350.00, 180, 0, 1, DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now)),
        (@ServiceBrakePads,  'SVC-004', 'Troca de pastilhas de freio',    'Substituição das pastilhas de freio dianteiras',             180.00, 50, 0, 1, DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now)),
        (@ServiceTimingBelt, 'SVC-005', 'Troca de correia dentada',       'Substituição da correia dentada e tensor',                    420.00, 150, 0, 1, DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now)),
        -- IsActive=0 de propósito: mostra um serviço descontinuado que ainda aparece no histórico de uma OS antiga (OS6)
        (@ServiceDiagnostic, 'SVC-006', 'Diagnóstico eletrônico',         'Leitura de códigos de falha via scanner automotivo',          90.00, 30, 0, 0, DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now));

    ------------------------------------------------------------------
    -- Catálogo de peças/insumos, com estoque. QuantityOnHand/ReservedQuantity já
    -- refletem o efeito acumulado (na ordem cronológica real) das reservas/baixas
    -- das Ordens de Serviço mais abaixo — ver o comentário de cada linha.
    ------------------------------------------------------------------
    DECLARE @ItemOil         UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555550001';
    DECLARE @ItemOilFilter   UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555550002';
    DECLARE @ItemBrakePad    UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555550003';
    DECLARE @ItemTimingBeltKit UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555550004';
    DECLARE @ItemBrakeFluid  UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555550005';
    DECLARE @ItemWiperBlade  UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555550006';

    INSERT INTO InventoryItems (Id, Code, Name, Description, QuantityOnHand, ReservedQuantity, MinimumStock, UnitPrice, UnitOfMeasure, IsActive, CreatedAt, UpdatedAt)
    VALUES
        -- Estoque inicial 50L. OS5 reservou/baixou 4L (Finalizada); OS3 reservou mais 4L, ainda em Aguardando aprovação -> QOH=46, Reserved=4
        (@ItemOil,           'PC-001', 'Óleo de motor 5W30 sintético',   'Óleo lubrificante sintético 1 litro',              46, 4, 10, 45.90, 'Liter', 1, DATEADD(DAY, -90, @Now), DATEADD(DAY, -4, @Now)),
        -- Estoque inicial 30un. OS5 reservou/baixou 1 (Finalizada); OS3 reservou mais 1, ainda em Aguardando aprovação -> QOH=29, Reserved=1
        (@ItemOilFilter,     'PC-002', 'Filtro de óleo',                 'Filtro de óleo compatível com motores 1.0 a 2.0',  29, 1, 8, 28.50, 'Piece', 1, DATEADD(DAY, -90, @Now), DATEADD(DAY, -4, @Now)),
        -- Estoque inicial 20un. OS4 reservou e já baixou 1 (aprovada, Em execução) -> QOH=19, Reserved=0
        (@ItemBrakePad,      'PC-003', 'Pastilha de freio dianteira',    'Jogo de pastilhas de freio dianteiras',            19, 0, 5, 95.00, 'Piece', 1, DATEADD(DAY, -90, @Now), DATEADD(DAY, -5, @Now)),
        -- Estoque inicial 12un. OS4 reservou e já baixou 1 (aprovada, Em execução) -> QOH=11, Reserved=0
        (@ItemTimingBeltKit, 'PC-004', 'Correia dentada',                'Kit correia dentada com tensor',                   11, 0, 3, 210.00, 'Piece', 1, DATEADD(DAY, -90, @Now), DATEADD(DAY, -5, @Now)),
        -- Estoque inicial 25L. OS5 reservou e já baixou 1L (Finalizada) -> QOH=24, Reserved=0
        (@ItemBrakeFluid,    'PC-005', 'Fluido de freio DOT4',           'Fluido de freio DOT4 500ml',                       24, 0, 5, 22.00, 'Liter', 1, DATEADD(DAY, -90, @Now), DATEADD(DAY, -12, @Now)),
        -- Nunca usado em nenhuma OS; abaixo do MinimumStock de propósito, pra demonstrar um cenário de estoque baixo
        (@ItemWiperBlade,    'PC-006', 'Palheta de limpador de para-brisa', 'Par de palhetas dianteiras',                    2, 0, 5, 35.00, 'Piece', 1, DATEADD(DAY, -90, @Now), DATEADD(DAY, -90, @Now));

    ------------------------------------------------------------------
    -- Ordens de Serviço — uma em cada status da máquina de estados, na ordem
    -- cronológica real (mais antiga primeiro): OS6 (30d atrás) -> OS5 (15d) ->
    -- OS4 (8d) -> OS3 (6d) -> OS2 (3d) -> OS1 (1d, a mais recente/aberta).
    ------------------------------------------------------------------
    DECLARE @Order1Received         UNIQUEIDENTIFIER = '66666666-6666-6666-6666-666666660001';
    DECLARE @Order2Diagnosing       UNIQUEIDENTIFIER = '66666666-6666-6666-6666-666666660002';
    DECLARE @Order3AwaitingApproval UNIQUEIDENTIFIER = '66666666-6666-6666-6666-666666660003';
    DECLARE @Order4InProgress       UNIQUEIDENTIFIER = '66666666-6666-6666-6666-666666660004';
    DECLARE @Order5Completed        UNIQUEIDENTIFIER = '66666666-6666-6666-6666-666666660005';
    DECLARE @Order6Delivered        UNIQUEIDENTIFIER = '66666666-6666-6666-6666-666666660006';

    INSERT INTO ServiceOrders (Id, CustomerId, VehicleId, CreatedBy, Status, ProblemDescription, DiagnoseDescription, OdometerReading, Discount, Subtotal, Total, OpenedAt, ClosedAt, CreatedAt, UpdatedAt)
    VALUES
        -- Recebida: acabou de abrir, sem diagnóstico nem orçamento ainda
        (@Order1Received, @CustomerMaria, @VehicleCivic, @UserAttendant, 'Received',
            'Barulho estranho no motor ao acelerar', NULL, 45000, 0, 0, 0,
            DATEADD(DAY, -1, @Now), NULL, DATEADD(DAY, -1, @Now), DATEADD(DAY, -1, @Now)),

        -- Em diagnóstico: já tem descrição do diagnóstico, orçamento ainda não foi montado
        (@Order2Diagnosing, @CustomerJoao, @VehicleArgo, @UserAttendant, 'Diagnosing',
            'Veículo puxando para a direita ao frear',
            'Identificado desgaste irregular das pastilhas dianteiras e possível problema no alinhamento',
            62000, 0, 0, 0,
            DATEADD(DAY, -3, @Now), NULL, DATEADD(DAY, -3, @Now), DATEADD(DAY, -2, @Now)),

        -- Aguardando aprovação: orçamento montado (Subtotal = 150.00 serviço + 183.60 + 28.50 peças = 362.10), estoque só reservado ainda
        (@Order3AwaitingApproval, @CustomerEstrela, @VehicleSaveiro, @UserAttendant, 'AwaitingApproval',
            'Revisão preventiva de rotina da frota',
            'Veículo em bom estado geral, recomendada troca de óleo e filtro preventiva',
            88000, 0, 362.10, 362.10,
            DATEADD(DAY, -6, @Now), NULL, DATEADD(DAY, -6, @Now), DATEADD(DAY, -4, @Now)),

        -- Em execução: orçamento aprovado (Subtotal = 180+420 serviços + 95+210 peças = 905.00), estoque já baixado de verdade
        (@Order4InProgress, @CustomerFernanda, @VehicleCorolla, @UserAttendant, 'InProgress',
            'Barulho ao frear e vibração no volante em alta velocidade',
            'Pastilhas de freio dianteiras desgastadas e correia dentada próxima do fim da vida útil, recomendada substituição preventiva',
            71000, 0, 905.00, 905.00,
            DATEADD(DAY, -8, @Now), NULL, DATEADD(DAY, -8, @Now), DATEADD(DAY, -5, @Now)),

        -- Finalizada: fluxo completo até a conclusão (Subtotal = 150+120 serviços + 183.60+28.50+22.00 peças = 504.10)
        (@Order5Completed, @CustomerMaria, @VehicleRenegade, @UserAttendant, 'Completed',
            'Revisão dos 20 mil km',
            'Revisão de rotina, troca de óleo, filtro e verificação geral',
            20500, 0, 504.10, 504.10,
            DATEADD(DAY, -15, @Now), NULL, DATEADD(DAY, -15, @Now), DATEADD(DAY, -10, @Now)),

        -- Entregue via reprovação: cliente reprovou o orçamento (Subtotal = 90.00, só diagnóstico), veículo retirado sem reparo
        (@Order6Delivered, @CustomerMaria, @VehicleCivic, @UserAttendant, 'Delivered',
            'Cliente relatou consumo elevado de combustível',
            'Diagnóstico eletrônico indicou sensor de oxigênio com falha; orçamento não aprovado pelo cliente',
            43000, 0, 90.00, 90.00,
            DATEADD(DAY, -30, @Now), DATEADD(DAY, -26, @Now), DATEADD(DAY, -30, @Now), DATEADD(DAY, -26, @Now));

    ------------------------------------------------------------------
    -- Peças "fotografadas" do catálogo em cada orçamento (Name/Description/UnitPrice
    -- congelados no momento do AddBudget, mesmo se o catálogo mudar depois)
    ------------------------------------------------------------------
    INSERT INTO ServiceOrderParts (Id, ServiceOrderId, InventoryItemId, Name, Description, UnitPrice, Quantity, CreatedAt, UpdatedAt)
    VALUES
        ('77777777-7777-7777-7777-777777770001', @Order3AwaitingApproval, @ItemOil,        'Óleo de motor 5W30 sintético', 'Óleo lubrificante sintético 1 litro',             45.90, 4, DATEADD(DAY, -4, @Now), DATEADD(DAY, -4, @Now)),
        ('77777777-7777-7777-7777-777777770002', @Order3AwaitingApproval, @ItemOilFilter,  'Filtro de óleo',               'Filtro de óleo compatível com motores 1.0 a 2.0', 28.50, 1, DATEADD(DAY, -4, @Now), DATEADD(DAY, -4, @Now)),
        ('77777777-7777-7777-7777-777777770003', @Order4InProgress,       @ItemBrakePad,   'Pastilha de freio dianteira', 'Jogo de pastilhas de freio dianteiras',           95.00, 1, DATEADD(DAY, -6, @Now), DATEADD(DAY, -6, @Now)),
        ('77777777-7777-7777-7777-777777770004', @Order4InProgress,       @ItemTimingBeltKit, 'Correia dentada',          'Kit correia dentada com tensor',                  210.00, 1, DATEADD(DAY, -6, @Now), DATEADD(DAY, -6, @Now)),
        ('77777777-7777-7777-7777-777777770005', @Order5Completed,        @ItemOil,        'Óleo de motor 5W30 sintético', 'Óleo lubrificante sintético 1 litro',             45.90, 4, DATEADD(DAY, -13, @Now), DATEADD(DAY, -13, @Now)),
        ('77777777-7777-7777-7777-777777770006', @Order5Completed,        @ItemOilFilter,  'Filtro de óleo',               'Filtro de óleo compatível com motores 1.0 a 2.0', 28.50, 1, DATEADD(DAY, -13, @Now), DATEADD(DAY, -13, @Now)),
        ('77777777-7777-7777-7777-777777770007', @Order5Completed,        @ItemBrakeFluid, 'Fluido de freio DOT4',         'Fluido de freio DOT4 500ml',                      22.00, 1, DATEADD(DAY, -13, @Now), DATEADD(DAY, -13, @Now));

    ------------------------------------------------------------------
    -- Serviços "fotografados" do catálogo em cada orçamento. EstimatedDuration é o
    -- valor do catálogo NO MOMENTO do orçamento (não o valor final calculado acima) —
    -- por isso o item da OS3 já reflete os 45min atualizados pela OS5, que aconteceu
    -- antes cronologicamente, enquanto o item da própria OS5 ainda mostra os 40min
    -- originais (valor antes da sua própria conclusão atualizar o catálogo).
    ------------------------------------------------------------------
    INSERT INTO ServiceOrderServices (Id, ServiceOrderId, ServiceId, Name, Description, UnitPrice, Quantity, EstimatedDuration, ActualDuration, CreatedAt, UpdatedAt)
    VALUES
        ('88888888-8888-8888-8888-888888880001', @Order3AwaitingApproval, @ServiceOilChange,  'Troca de óleo e filtro',      'Troca de óleo do motor e filtro de óleo',                  150.00, 1, 45, NULL, DATEADD(DAY, -4, @Now), DATEADD(DAY, -4, @Now)),
        ('88888888-8888-8888-8888-888888880002', @Order4InProgress,       @ServiceBrakePads,  'Troca de pastilhas de freio', 'Substituição das pastilhas de freio dianteiras',           180.00, 1, 50, NULL, DATEADD(DAY, -6, @Now), DATEADD(DAY, -6, @Now)),
        ('88888888-8888-8888-8888-888888880003', @Order4InProgress,       @ServiceTimingBelt, 'Troca de correia dentada',    'Substituição da correia dentada e tensor',                 420.00, 1, 150, NULL, DATEADD(DAY, -6, @Now), DATEADD(DAY, -6, @Now)),
        ('88888888-8888-8888-8888-888888880004', @Order5Completed,        @ServiceOilChange,  'Troca de óleo e filtro',      'Troca de óleo do motor e filtro de óleo',                  150.00, 1, 40, 45,   DATEADD(DAY, -13, @Now), DATEADD(DAY, -10, @Now)),
        ('88888888-8888-8888-8888-888888880005', @Order5Completed,        @ServiceAlignment,  'Alinhamento e balanceamento', 'Alinhamento de direção e balanceamento das quatro rodas', 120.00, 1, 60, 55,   DATEADD(DAY, -13, @Now), DATEADD(DAY, -10, @Now)),
        ('88888888-8888-8888-8888-888888880006', @Order6Delivered,        @ServiceDiagnostic, 'Diagnóstico eletrônico',      'Leitura de códigos de falha via scanner automotivo',       90.00, 1, 30, NULL, DATEADD(DAY, -28, @Now), DATEADD(DAY, -28, @Now));

    ------------------------------------------------------------------
    -- Histórico de status — uma linha por transição já ocorrida (Recebida->X
    -- nunca aparece aqui porque a criação da OS não é uma transição registrada,
    -- só as mudanças feitas pelos use cases StartDiagnosis/AddBudget/Approve/
    -- Reject/Complete/Deliver — ver ServiceOrder em CONTEXT.md §3).
    ------------------------------------------------------------------
    INSERT INTO ServiceOrderStatusHistories (Id, ServiceOrderId, PreviousStatus, CurrentStatus, ChangedBy, ChangedAt)
    VALUES
        ('99999999-9999-9999-9999-999999990001', @Order2Diagnosing,       'Received',         'Diagnosing',       @UserMechanic,  DATEADD(DAY, -2, @Now)),

        ('99999999-9999-9999-9999-999999990002', @Order3AwaitingApproval, 'Received',         'Diagnosing',       @UserMechanic,  DATEADD(DAY, -5, @Now)),
        ('99999999-9999-9999-9999-999999990003', @Order3AwaitingApproval, 'Diagnosing',       'AwaitingApproval', @UserMechanic,  DATEADD(DAY, -4, @Now)),

        ('99999999-9999-9999-9999-999999990004', @Order4InProgress,       'Received',         'Diagnosing',       @UserMechanic,  DATEADD(DAY, -7, @Now)),
        ('99999999-9999-9999-9999-999999990005', @Order4InProgress,       'Diagnosing',       'AwaitingApproval', @UserMechanic,  DATEADD(DAY, -6, @Now)),
        ('99999999-9999-9999-9999-999999990006', @Order4InProgress,       'AwaitingApproval', 'InProgress',       @UserAttendant, DATEADD(DAY, -5, @Now)),

        ('99999999-9999-9999-9999-999999990007', @Order5Completed,        'Received',         'Diagnosing',       @UserMechanic,  DATEADD(DAY, -14, @Now)),
        ('99999999-9999-9999-9999-999999990008', @Order5Completed,        'Diagnosing',       'AwaitingApproval', @UserMechanic,  DATEADD(DAY, -13, @Now)),
        ('99999999-9999-9999-9999-999999990009', @Order5Completed,        'AwaitingApproval', 'InProgress',       @UserAttendant, DATEADD(DAY, -12, @Now)),
        ('99999999-9999-9999-9999-999999990010', @Order5Completed,        'InProgress',       'Completed',        @UserMechanic,  DATEADD(DAY, -10, @Now)),

        ('99999999-9999-9999-9999-999999990011', @Order6Delivered,        'Received',         'Diagnosing',       @UserMechanic,  DATEADD(DAY, -29, @Now)),
        ('99999999-9999-9999-9999-999999990012', @Order6Delivered,        'Diagnosing',       'AwaitingApproval', @UserMechanic,  DATEADD(DAY, -28, @Now)),
        ('99999999-9999-9999-9999-999999990013', @Order6Delivered,        'AwaitingApproval', 'Cancelled',        @UserAttendant, DATEADD(DAY, -27, @Now)),
        ('99999999-9999-9999-9999-999999990014', @Order6Delivered,        'Cancelled',        'Delivered',        @UserAttendant, DATEADD(DAY, -26, @Now));
END
GO
