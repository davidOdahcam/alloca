/* =====================================================================
   Seed de recursos - Pavimento BCP01 (Biblioteca Central - Pavimento 1)
   Gerado a partir de recursos.md e seed-data.json.

   Pre-requisitos:
     - O Pavilhao 'BCP' e o Andar 'BCP01' ja devem existir
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
    WHERE p.Code = 'BCP' AND f.Code = 'BCP01'
);

IF @FloorId IS NULL
    THROW 50001, 'Andar BCP01 do pavilhao BCP nao encontrado. Rode o seed da aplicacao antes.', 1;

/* ------------------------------- SALAS ------------------------------- */
INSERT INTO Rooms (Id, FloorId, ExternalId, Name, IsReservable, Capacity, CreatedAt, UpdatedAt)
SELECT NEWID(), @FloorId, v.ExternalId, v.Name, v.IsReservable, v.Capacity, SYSUTCDATETIME(), NULL
FROM (VALUES
    ('ROOM-BCP01S001', N'Hall de entrada',          0,   0),
    ('ROOM-BCP01S002', N'Guarda-volumes',           0,   0),
    ('ROOM-BCP01S003', N'Exposicao',                0,   3),
    ('ROOM-BCP01S004', N'Secretaria',               0,   2),
    ('ROOM-BCP01S005', N'Chefia',                   0,   2),
    ('ROOM-BCP01S006', N'Deposito',                 0,   0),
    ('ROOM-BCP01S007', N'Circulacao',               0,   0),
    ('ROOM-BCP01S008', N'Criacao visual revisao',   0,  15),
    ('ROOM-BCP01S009', N'Deposito',                 0,   0),
    ('ROOM-BCP01S010', N'Reuniao',                  0,   8),
    ('ROOM-BCP01S011', N'Copa',                     0,   0),
    ('ROOM-BCP01S012', N'Sanitario',                0,   0),
    ('ROOM-BCP01S013', N'Hall de acesso',           0,   0),
    ('ROOM-BCP01S014', N'Recepcao',                 0,   0),
    ('ROOM-BCP01S015', N'Sanitario',                0,   0),
    ('ROOM-BCP01S016', N'Banheiro masculino',       0,   0),
    ('ROOM-BCP01S017', N'Banheiro feminino',        0,   0),
    ('ROOM-BCP01S018', N'E-books',                  0,  24),
    ('ROOM-BCP01S019', N'Referencia',               0,   5),
    ('ROOM-BCP01S020', N'Coinfo',                   0,   0),
    ('ROOM-BCP01S021', N'Emprestimo',               0,   0),
    ('ROOM-BCP01S022', N'Restauro',                 0,   8),
    ('ROOM-BCP01S023', N'Circulacao',               0,   0),
    ('ROOM-BCP01S024', N'Copa dos servidores',      0,   8),
    ('ROOM-BCP01S025', N'DML',                      0,   0),
    ('ROOM-BCP01S026', N'Banheiro feminino',        0,   0),
    ('ROOM-BCP01S027', N'Banheiro masculino',       0,   0),
    ('ROOM-BCP01S028', N'Deposito',                 0,   0),
    ('ROOM-BCP01S029', N'Processamento tecnico',    0,  26),
    ('ROOM-BCP01S030', N'Chefia',                   0,   3),
    ('ROOM-BCP01S031', N'Periodicos',               0,   0),
    ('ROOM-BCP01S032', N'Secretaria',               0,   6),
    ('ROOM-BCP01S033', N'Chefia',                   0,   3),
    ('ROOM-BCP01S034', N'Acervo/Leitura',           0, 150),
    ('ROOM-BCP01S035', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP01S036', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP01S037', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP01S038', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP01S039', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP01S040', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP01S041', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP01S042', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP01S043', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP01S044', N'Cabine de estudo',         1,   4),
    ('ROOM-BCP01S045', N'Cabine de estudo',         1,   6),
    ('ROOM-BCP01S046', N'Banheiro feminino',        0,   0),
    ('ROOM-BCP01S047', N'Banheiro masculino',       0,   0),
    ('ROOM-BCP01S048', N'Sanitario',                0,   0),
    ('ROOM-BCP01S049', N'Sanitario',                0,   0)
) AS v(ExternalId, Name, IsReservable, Capacity)
WHERE NOT EXISTS (SELECT 1 FROM Rooms r WHERE r.ExternalId = v.ExternalId);

/* ------------------------------- MESAS -------------------------------
   Cada linha descreve uma "serie" de mesas:
     (Sala, Prefixo, NumeroInicial, Quantidade, IsReservable)
   O ExternalId final fica: Prefixo + numero com 5 digitos (zero a esq).
   Ex.: ('...', 'DESK-RTBO-', 12, 24, 1) -> DESK-RTBO-00012 .. DESK-RTBO-00035
   --------------------------------------------------------------------- */
;WITH series(RoomExternalId, Prefix, StartNum, Cnt, IsReservable) AS (
    SELECT * FROM (VALUES
        ('ROOM-BCP01S003', 'DESK-LLAO-',  1,  1, 0),
        ('ROOM-BCP01S004', 'DESK-LLCO-',  1,  2, 0),
        ('ROOM-BCP01S005', 'DESK-LLBO-',  1,  2, 0),
        ('ROOM-BCP01S008', 'DESK-RDAF-',  1,  1, 0),
        ('ROOM-BCP01S008', 'DESK-RTBO-',  1, 11, 0),
        ('ROOM-BCP01S010', 'DESK-RTAX-',  1,  1, 0),
        ('ROOM-BCP01S018', 'DESK-RTBO-', 12, 24, 1),
        ('ROOM-BCP01S019', 'DESK-RDAF-',  2,  1, 0),
        ('ROOM-BCP01S019', 'DESK-LLAO-',  2,  1, 0),
        ('ROOM-BCP01S022', 'DESK-RTAX-',  2,  1, 0),
        ('ROOM-BCP01S024', 'DESK-RDAF-',  3,  2, 0),
        ('ROOM-BCP01S029', 'DESK-RTAX-',  3,  1, 0),
        ('ROOM-BCP01S029', 'DESK-LLAO-',  3, 10, 0),
        ('ROOM-BCP01S030', 'DESK-LLAO-', 13,  1, 0),
        ('ROOM-BCP01S032', 'DESK-LLAO-', 14,  2, 0),
        ('ROOM-BCP01S033', 'DESK-LLAO-', 16,  1, 0),
        ('ROOM-BCP01S034', 'DESK-RDAF-',  5, 35, 1),
        ('ROOM-BCP01S034', 'DESK-RTBO-', 36,  8, 1),
        ('ROOM-BCP01S035', 'DESK-RDAF-', 40,  1, 1),
        ('ROOM-BCP01S036', 'DESK-RDAF-', 41,  1, 1),
        ('ROOM-BCP01S037', 'DESK-RDAF-', 42,  1, 1),
        ('ROOM-BCP01S038', 'DESK-RDAF-', 43,  1, 1),
        ('ROOM-BCP01S039', 'DESK-RDAF-', 44,  1, 1),
        ('ROOM-BCP01S040', 'DESK-RDAF-', 45,  1, 1),
        ('ROOM-BCP01S041', 'DESK-RDAF-', 46,  1, 1),
        ('ROOM-BCP01S042', 'DESK-RDAF-', 47,  1, 1),
        ('ROOM-BCP01S043', 'DESK-RDAF-', 48,  1, 1),
        ('ROOM-BCP01S044', 'DESK-RDAF-', 49,  1, 1),
        ('ROOM-BCP01S045', 'DESK-RTAS-',  1,  1, 1)
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

PRINT 'BCP01: salas e mesas inseridas com sucesso.';
