/* =====================================================================
   Seed inicial - Usuarios, Pavilhao BCP, Horarios e Andares
   Gerado a partir de seed-data.json.

   Caracteristicas:
     - Idempotente: usa NOT EXISTS / IF NOT EXISTS em cada insercao.
     - Deve ser executado ANTES de BCP01.sql e BCP02.sql.
     - Senhas em PBKDF2: v1.100000.<salt>.<hash>
         admin@alloca.local   -> Admin@123
         gestor@alloca.local  -> Gestor@123
         aluno@alloca.local   -> Aluno@123
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

/* ================================================================
   1. USUARIOS
   UserRole: Member=1, PavilionManager=2, Admin=3
   ================================================================ */

-- admin@alloca.local (Admin)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@alloca.local')
    INSERT INTO Users (Id, Email, FullName, PasswordHash, Role, IsActive, CreatedAt, UpdatedAt)
    VALUES (
        NEWID(),
        'admin@alloca.local',
        N'Administrador',
        'v1.100000.hctXZilMkaB+7IGobIdoCg==.j6JnXN27G8Pj8rvDjfqubo/KSsj7lVRjhPwSCUuULnw=',
        3, -- Admin
        1,
        SYSUTCDATETIME(),
        NULL
    );

-- gestor@alloca.local (PavilionManager)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'gestor@alloca.local')
    INSERT INTO Users (Id, Email, FullName, PasswordHash, Role, IsActive, CreatedAt, UpdatedAt)
    VALUES (
        NEWID(),
        'gestor@alloca.local',
        N'Gestor BCP',
        'v1.100000.evDuUusFUCMLRkfpDOsKnA==.4EqPLCSTSVjK2H+C3YR9uIWv3CbElpNuFiZSbgHYTsw=',
        2, -- PavilionManager
        1,
        SYSUTCDATETIME(),
        NULL
    );

-- aluno@alloca.local (Member)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'aluno@alloca.local')
    INSERT INTO Users (Id, Email, FullName, PasswordHash, Role, IsActive, CreatedAt, UpdatedAt)
    VALUES (
        NEWID(),
        'aluno@alloca.local',
        N'Aluno Teste',
        'v1.100000.ngy5O2wSk78pUm+glbIaiw==.X6DV3LCWJO3jJND+EVs0hQ5dbQQkhMm4+Ye8lPBLpH8=',
        1, -- Member
        1,
        SYSUTCDATETIME(),
        NULL
    );

/* ================================================================
   2. PAVILHAO BCP
   ================================================================ */

IF NOT EXISTS (SELECT 1 FROM Pavilions WHERE Code = 'BCP')
    INSERT INTO Pavilions (Id, Code, Name, MinAdvanceMinutes, MaxAdvanceDays, SlotMinutes, CreatedAt, UpdatedAt)
    VALUES (NEWID(), 'BCP', N'Biblioteca Central', 5, 30, 30, SYSUTCDATETIME(), NULL);

/* ================================================================
   3. HORARIOS DE FUNCIONAMENTO
   DayOfWeek: Sunday=0, Monday=1, Tuesday=2, Wednesday=3,
              Thursday=4, Friday=5, Saturday=6
   IsClosed = 0 (aberto) em todos os casos do seed.
   ================================================================ */

;WITH dias(DayOfWeek, OpensAt, ClosesAt) AS (
    SELECT * FROM (VALUES
        (1, '08:00:00', '20:00:00'), -- Monday
        (2, '08:00:00', '20:00:00'), -- Tuesday
        (3, '08:00:00', '20:00:00'), -- Wednesday
        (4, '08:00:00', '20:00:00'), -- Thursday
        (5, '08:00:00', '20:00:00'), -- Friday
        (6, '08:00:00', '17:00:00')  -- Saturday
    ) AS v(DayOfWeek, OpensAt, ClosesAt)
)
INSERT INTO OperatingHours (Id, PavilionId, DayOfWeek, OpensAt, ClosesAt, IsClosed, CreatedAt, UpdatedAt)
SELECT NEWID(), p.Id, d.DayOfWeek, CAST(d.OpensAt AS time), CAST(d.ClosesAt AS time), 0, SYSUTCDATETIME(), NULL
FROM dias d
INNER JOIN Pavilions p ON p.Code = 'BCP'
WHERE NOT EXISTS (
    SELECT 1 FROM OperatingHours oh
    WHERE oh.PavilionId = p.Id AND oh.DayOfWeek = d.DayOfWeek
);

/* ================================================================
   4. ANDARES
   ================================================================ */

INSERT INTO Floors (Id, PavilionId, Code, Name, Level, SvgKey, CreatedAt, UpdatedAt)
SELECT NEWID(), p.Id, v.Code, v.Name, v.Level, v.SvgKey, SYSUTCDATETIME(), NULL
FROM Pavilions p
CROSS JOIN (VALUES
    ('BCP01', N'Biblioteca Central - Pavimento 1', 1, 'bcp01'),
    ('BCP02', N'Biblioteca Central - Pavimento 2', 2, 'bcp02')
) AS v(Code, Name, Level, SvgKey)
WHERE p.Code = 'BCP'
  AND NOT EXISTS (SELECT 1 FROM Floors f WHERE f.PavilionId = p.Id AND f.Code = v.Code);

/* ================================================================
   5. GESTORES DO PAVILHAO
   Associa admin e gestor ao pavilhao BCP na tabela PavilionManagers.
   ================================================================ */

INSERT INTO PavilionManagers (Id, PavilionId, UserId, AssignedAt, CreatedAt, UpdatedAt)
SELECT NEWID(), p.Id, u.Id, SYSUTCDATETIME(), SYSUTCDATETIME(), NULL
FROM Pavilions p
INNER JOIN Users u ON u.Email IN ('admin@alloca.local', 'gestor@alloca.local')
WHERE p.Code = 'BCP'
  AND NOT EXISTS (
      SELECT 1 FROM PavilionManagers pm
      WHERE pm.PavilionId = p.Id AND pm.UserId = u.Id
  );

COMMIT TRANSACTION;

PRINT 'Seed inicial (usuarios, pavilhao, horarios, andares, gestores) inserido com sucesso.';
