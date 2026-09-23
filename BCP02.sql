/* =====================================================================
   Seed de recursos - Pavimento BCP02 (Biblioteca Central - Pavimento 2)
   Gerado a partir de recursos.md e seed-data.json.

   Pre-requisitos:
     - O Pavilhao 'BCP' e o Andar 'BCP02' ja devem existir
       (criados pelo seed da aplicacao / seed-data.json).

   Caracteristicas:
     - Idempotente: pode ser executado mais de uma vez sem duplicar
       (usa NOT EXISTS por ExternalId, que possui indice unico).
     - Mesas (Desks) usam o proprio ExternalId como Name.
     - IsReservable: 1 = reservavel, 0 = nao reservavel.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @FloorId uniqueidentifier = (
    SELECT f.Id
    FROM Floors f
    INNER JOIN Pavilions p ON p.Id = f.PavilionId
    WHERE p.Code = 'BCP' AND f.Code = 'BCP02'
);

IF @FloorId IS NULL
    THROW 50002, 'Andar BCP02 do pavilhao BCP nao encontrado. Rode o seed da aplicacao antes.', 1;

/* ------------------------------- SALAS ------------------------------- */
INSERT INTO Rooms (Id, FloorId, ExternalId, Name, IsReservable, Capacity, CreatedAt, UpdatedAt)
SELECT NEWID(), @FloorId, v.ExternalId, v.Name, v.IsReservable, v.Capacity, SYSUTCDATETIME(), NULL
FROM (VALUES
    ('ROOM-BCP02S001', N'Hall de acesso',           0,   0),
    ('ROOM-BCP02S002', N'Auditorio',                0,  82),
    ('ROOM-BCP02S003', N'Recepcao',                 0,   3),
    ('ROOM-BCP02S004', N'Circulacao',               0,   0),
    ('ROOM-BCP02S005', N'Diretoria',                0,   9),
    ('ROOM-BCP02S006', N'Banheiro',                 0,   0),
    ('ROOM-BCP02S007', N'Secretaria executiva',     0,   3),
    ('ROOM-BCP02S008', N'Sala de reunioes',         0,   9),
    ('ROOM-BCP02S009', N'Copa',                     0,   0),
    ('ROOM-BCP02S010', N'Sanitario',                0,   0),
    ('ROOM-BCP02S011', N'Sanitario',                0,   0),
    ('ROOM-BCP02S012', N'Banheiro masculino',       0,   0),
    ('ROOM-BCP02S013', N'Banheiro feminino',        0,   0),
    ('ROOM-BCP02S014', N'Cabine de estudo',         1,   6),
    ('ROOM-BCP02S015', N'Cabine de estudo',         1,   6),
    ('ROOM-BCP02S016', N'Cabine de estudo',         1,   6),
    ('ROOM-BCP02S017', N'Periodicos',               0,  24),
    ('ROOM-BCP02S018', N'Sala de aula',             1,  30),
    ('ROOM-BCP02S019', N'Sala de aula',             1,  30),
    ('ROOM-BCP02S020', N'Periodicos eletronicos',   0,  24),
    ('ROOM-BCP02S021', N'Multimeios',               1,  30),
    ('ROOM-BCP02S022', N'Multimeios',               0,  20),
    ('ROOM-BCP02S023', N'Acervo/Leitura',           0, 200),
    ('ROOM-BCP02S024', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP02S025', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP02S026', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP02S027', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP02S028', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP02S029', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP02S030', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP02S031', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP02S032', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP02S033', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP02S034', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP02S035', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP02S036', N'Sanitario',                0,   0),
    ('ROOM-BCP02S037', N'Sanitario',                0,   0),
    ('ROOM-BCP02S038', N'Banheiro masculino',       0,   0),
    ('ROOM-BCP02S039', N'Banheiro feminino',        0,   0),
    ('ROOM-BCP02S040', N'Chefia',                   0,  10),
    ('ROOM-BCP02S041', N'Teses',                    0,   0),
    ('ROOM-BCP02S042', N'Banheiro feminino',        0,   0),
    ('ROOM-BCP02S043', N'Banheiro masculino',       0,   0)
) AS v(ExternalId, Name, IsReservable, Capacity)
WHERE NOT EXISTS (SELECT 1 FROM Rooms r WHERE r.ExternalId = v.ExternalId);

/* ------------------------------- MESAS -------------------------------
   Cada linha descreve uma "serie" de mesas:
     (Sala, Prefixo, NumeroInicial, Quantidade, IsReservable)
   O ExternalId final fica: Prefixo + numero com 5 digitos (zero a esq).
   Ex.: ('...', 'DESK-ADAO-', 1, 77, 1) -> DESK-ADAO-00001 .. DESK-ADAO-00077
   --------------------------------------------------------------------- */
;WITH series(RoomExternalId, Prefix, StartNum, Cnt, IsReservable) AS (
    SELECT * FROM (VALUES
        ('ROOM-BCP02S002', 'DESK-ADAO-',   1, 77, 0),
        ('ROOM-BCP02S002', 'DESK-ADBO-',   1,  1, 0),
        ('ROOM-BCP02S003', 'DESK-LLAO-',  17,  1, 0),
        ('ROOM-BCP02S005', 'DESK-LLAO-',  18,  1, 0),
        ('ROOM-BCP02S005', 'DESK-RTAS-',   2,  1, 0),
        ('ROOM-BCP02S007', 'DESK-LLAO-',  19,  1, 0),
        ('ROOM-BCP02S008', 'DESK-LLAO-',  20,  1, 0),
        ('ROOM-BCP02S008', 'DESK-RTAS-',   3,  1, 0),
        ('ROOM-BCP02S014', 'DESK-RTAS-',   4,  1, 1),
        ('ROOM-BCP02S015', 'DESK-RTAS-',   5,  1, 1),
        ('ROOM-BCP02S016', 'DESK-RTAS-',   6,  1, 1),
        ('ROOM-BCP02S017', 'DESK-RDAF-',  50,  6, 1),
        ('ROOM-BCP02S020', 'DESK-RTBO-',  44, 24, 0),
        ('ROOM-BCP02S021', 'DESK-CTAO-',   1, 28, 0),
        ('ROOM-BCP02S022', 'DESK-CTAO-',  29, 17, 0),
        ('ROOM-BCP02S023', 'DESK-RDAF-',  56, 43, 1),
        ('ROOM-BCP02S023', 'DESK-RTBO-',  68, 13, 1),
        ('ROOM-BCP02S024', 'DESK-RDAF-',  99,  1, 1),
        ('ROOM-BCP02S025', 'DESK-RDAF-', 100,  1, 1),
        ('ROOM-BCP02S026', 'DESK-RDAF-', 101,  1, 1),
        ('ROOM-BCP02S027', 'DESK-RDAF-', 102,  1, 1),
        ('ROOM-BCP02S028', 'DESK-RDAF-', 103,  1, 1),
        ('ROOM-BCP02S029', 'DESK-RDAF-', 104,  1, 1),
        ('ROOM-BCP02S030', 'DESK-RDAF-', 105,  1, 1),
        ('ROOM-BCP02S031', 'DESK-RDAF-', 106,  1, 1),
        ('ROOM-BCP02S032', 'DESK-RDAF-', 107,  1, 1),
        ('ROOM-BCP02S033', 'DESK-RDAF-', 108,  1, 1),
        ('ROOM-BCP02S034', 'DESK-RDAF-', 109,  1, 1),
        ('ROOM-BCP02S035', 'DESK-RTAS-',   7,  1, 1),
        ('ROOM-BCP02S040', 'DESK-LLAO-',  21,  2, 0),
        ('ROOM-BCP02S040', 'DESK-RDAF-', 110,  1, 0)
    ) AS v(RoomExternalId, Prefix, StartNum, Cnt, IsReservable)
),
nums AS (
    SELECT TOP (200) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS k
    FROM sys.all_objects
)
INSERT INTO Desks (Id, RoomId, ExternalId, Name, IsReservable, CreatedAt, UpdatedAt)
SELECT NEWID(), r.Id, e.ExternalId, e.ExternalId, s.IsReservable, SYSUTCDATETIME(), NULL
FROM series s
INNER JOIN nums n ON n.k < s.Cnt
INNER JOIN Rooms r ON r.ExternalId = s.RoomExternalId
CROSS APPLY (SELECT s.Prefix + RIGHT('00000' + CAST(s.StartNum + n.k AS varchar(5)), 5) AS ExternalId) e
WHERE NOT EXISTS (SELECT 1 FROM Desks d WHERE d.ExternalId = e.ExternalId);

COMMIT TRANSACTION;

PRINT 'BCP02: salas e mesas inseridas com sucesso.';
