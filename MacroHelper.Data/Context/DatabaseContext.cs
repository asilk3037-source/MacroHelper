using Microsoft.Data.Sqlite;

namespace MacroHelper.Data.Context;

public class DatabaseContext
{
    private string _connectionString = string.Empty;

    public DatabaseContext()
    {
        var caminho = DatabaseConfig.ObterCaminhoAtivo();
        Inicializar(caminho);
    }

    public SqliteConnection GetConnection() => new(_connectionString);

    public async Task<bool> VerificarIntegridadeAsync()
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            var resultado = (string?)await cmd.ExecuteScalarAsync();
            return string.Equals(resultado, "ok", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    public void TrocarCaminho(string novoCaminho)
    {
        Inicializar(novoCaminho);
        DatabaseConfig.SalvarCaminho(novoCaminho);
    }

    private void Inicializar(string caminhoDB)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(caminhoDB)!);
        _connectionString = $"Data Source={caminhoDB}";
        CriarTabelas();
    }

    private void CriarTabelas()
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            PRAGMA journal_mode=WAL;

            CREATE TABLE IF NOT EXISTS Usuarios (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome         TEXT NOT NULL,
                Email        TEXT NOT NULL UNIQUE,
                SenhaHash    TEXT NOT NULL,
                Perfil       TEXT NOT NULL DEFAULT 'Usuario',
                Ativo        INTEGER NOT NULL DEFAULT 1,
                DataCriacao  TEXT NOT NULL
            );

            INSERT OR IGNORE INTO Usuarios (Nome,Email,SenhaHash,Perfil,Ativo,DataCriacao)
            VALUES ('Administrador','admin@macrohelper.com',
                '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9',
                'Admin',1,datetime('now'));

            CREATE TABLE IF NOT EXISTS Categorias (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome        TEXT NOT NULL,
                Icone       TEXT,
                Cor         TEXT,
                PaiId       INTEGER REFERENCES Categorias(Id) ON DELETE SET NULL,
                Ordem       INTEGER NOT NULL DEFAULT 0,
                DataCriacao TEXT NOT NULL
            );

            INSERT OR IGNORE INTO Categorias (Nome,Icone,Cor,PaiId,Ordem,DataCriacao)
            SELECT 'Suporte','','#6C5CE7',NULL,1,datetime('now')
            WHERE NOT EXISTS (SELECT 1 FROM Categorias WHERE Nome='Suporte');

            INSERT OR IGNORE INTO Categorias (Nome,Icone,Cor,PaiId,Ordem,DataCriacao)
            SELECT 'Desenvolvimento','','#00b894',NULL,2,datetime('now')
            WHERE NOT EXISTS (SELECT 1 FROM Categorias WHERE Nome='Desenvolvimento');

            INSERT OR IGNORE INTO Categorias (Nome,Icone,Cor,PaiId,Ordem,DataCriacao)
            SELECT 'Atendimento','','#e17055',NULL,3,datetime('now')
            WHERE NOT EXISTS (SELECT 1 FROM Categorias WHERE Nome='Atendimento');

            CREATE TABLE IF NOT EXISTS Macros (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                Atalho          TEXT NOT NULL,
                Titulo          TEXT NOT NULL,
                Conteudo        TEXT NOT NULL,
                Categoria       TEXT,
                CategoriaId     INTEGER REFERENCES Categorias(Id) ON DELETE SET NULL,
                Ativo           INTEGER NOT NULL DEFAULT 1,
                DataCriacao     TEXT NOT NULL,
                DataAtualizacao TEXT
            );

            CREATE INDEX IF NOT EXISTS idx_macros_atalho ON Macros(Atalho);

            INSERT OR IGNORE INTO Macros (Atalho,Titulo,Conteudo,Categoria,Ativo,DataCriacao)
            SELECT 'chamado-recebido','Chamado Recebido',
                'Boa tarde, {nome}!' || char(10) || char(10) ||
                'Seu chamado foi recebido. Protocolo: {protocolo}' || char(10) || char(10) ||
                'Retornaremos em breve.' || char(10) || char(10) ||
                'Atenciosamente,' || char(10) || 'Equipe de Suporte',
                'Suporte',1,datetime('now')
            WHERE NOT EXISTS (SELECT 1 FROM Macros WHERE Atalho='chamado-recebido');

            INSERT OR IGNORE INTO Macros (Atalho,Titulo,Conteudo,Categoria,Ativo,DataCriacao)
            SELECT 'aguardando-retorno','Aguardando Retorno',
                'Prezado(a),' || char(10) || char(10) ||
                'Estamos aguardando seu retorno. Data do contato: {data}' || char(10) || char(10) ||
                'Atenciosamente,' || char(10) || 'Equipe de Suporte',
                'Suporte',1,datetime('now')
            WHERE NOT EXISTS (SELECT 1 FROM Macros WHERE Atalho='aguardando-retorno');

            CREATE TABLE IF NOT EXISTS LogUso (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                MacroId     INTEGER REFERENCES Macros(Id) ON DELETE SET NULL,
                MacroTitulo TEXT NOT NULL,
                MacroAtalho TEXT NOT NULL,
                Aplicativo  TEXT,
                UsuarioId   INTEGER REFERENCES Usuarios(Id) ON DELETE SET NULL,
                DataUso     TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_log_data ON LogUso(DataUso);

            CREATE TABLE IF NOT EXISTS MacroVersoes (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                MacroId         INTEGER NOT NULL REFERENCES Macros(Id) ON DELETE CASCADE,
                Titulo          TEXT NOT NULL,
                Conteudo        TEXT NOT NULL,
                ModificadoPorId INTEGER REFERENCES Usuarios(Id) ON DELETE SET NULL,
                DataModificacao TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_versoes_macro ON MacroVersoes(MacroId);

            CREATE TABLE IF NOT EXISTS LogAuditoria (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                UsuarioId   INTEGER REFERENCES Usuarios(Id) ON DELETE SET NULL,
                Acao        TEXT NOT NULL,
                Entidade    TEXT NOT NULL,
                EntidadeId  INTEGER,
                Detalhes    TEXT,
                Data        TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_auditoria_data ON LogAuditoria(Data);

            CREATE TABLE IF NOT EXISTS Grupos (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome        TEXT NOT NULL UNIQUE,
                DataCriacao TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS LogUsoResumo (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                AnoMes      TEXT NOT NULL,
                MacroTitulo TEXT NOT NULL,
                MacroAtalho TEXT NOT NULL,
                Total       INTEGER NOT NULL,
                Caracteres  INTEGER NOT NULL DEFAULT 0,
                UNIQUE(AnoMes, MacroAtalho)
            );

            CREATE TABLE IF NOT EXISTS FavoritosUsuario (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                UsuarioId   INTEGER NOT NULL REFERENCES Usuarios(Id) ON DELETE CASCADE,
                MacroId     INTEGER NOT NULL REFERENCES Macros(Id) ON DELETE CASCADE,
                DataCriacao TEXT NOT NULL,
                UNIQUE(UsuarioId, MacroId)
            );

            CREATE TABLE IF NOT EXISTS VariaveisGlobais (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome        TEXT NOT NULL UNIQUE,
                ValorPadrao TEXT NOT NULL DEFAULT '',
                Descricao   TEXT,
                DataCriacao TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS MacroAgendamentos (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                MacroId         INTEGER NOT NULL REFERENCES Macros(Id) ON DELETE CASCADE,
                DiasSemana      TEXT NOT NULL,
                Horario         TEXT NOT NULL,
                Ativo           INTEGER NOT NULL DEFAULT 1,
                UltimaExecucao  TEXT,
                DataCriacao     TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Webhooks (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome        TEXT NOT NULL,
                Url         TEXT NOT NULL,
                Evento      TEXT NOT NULL DEFAULT 'MacroUsada',
                Ativo       INTEGER NOT NULL DEFAULT 1,
                DataCriacao TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Notificacoes (
                Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                Titulo   TEXT NOT NULL,
                Mensagem TEXT NOT NULL,
                Tipo     TEXT NOT NULL DEFAULT 'Info',
                Lida     INTEGER NOT NULL DEFAULT 0,
                Data     TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_notificacoes_data ON Notificacoes(Data);

            CREATE TABLE IF NOT EXISTS MacroCurtidas (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                MacroId     INTEGER NOT NULL REFERENCES Macros(Id) ON DELETE CASCADE,
                UsuarioId   INTEGER NOT NULL REFERENCES Usuarios(Id) ON DELETE CASCADE,
                DataCriacao TEXT NOT NULL,
                UNIQUE(MacroId, UsuarioId)
            );
        ";
        cmd.ExecuteNonQuery();

        AplicarMigracoes(conn);
    }

    // Adiciona colunas novas em bancos já existentes (ALTER TABLE não é coberto pelo CREATE TABLE IF NOT EXISTS)
    private static void AplicarMigracoes(SqliteConnection conn)
    {
        AdicionarColunaSeNaoExiste(conn, "Macros",   "Favorito",             "INTEGER NOT NULL DEFAULT 0");
        AdicionarColunaSeNaoExiste(conn, "Macros",   "AtalhoTecla",          "TEXT");
        AdicionarColunaSeNaoExiste(conn, "Macros",   "Status",               "TEXT NOT NULL DEFAULT 'Aprovada'");
        AdicionarColunaSeNaoExiste(conn, "Macros",   "CriadoPorId",          "INTEGER");
        AdicionarColunaSeNaoExiste(conn, "Usuarios", "CategoriasPermitidas", "TEXT");
        AdicionarColunaSeNaoExiste(conn, "LogUso",   "Caracteres",           "INTEGER NOT NULL DEFAULT 0");
        AdicionarColunaSeNaoExiste(conn, "Macros",   "GruposPermitidos",     "TEXT");
        AdicionarColunaSeNaoExiste(conn, "Macros",   "ImagemBase64",         "TEXT");
        AdicionarColunaSeNaoExiste(conn, "Usuarios", "GrupoId",              "INTEGER");
        AdicionarColunaSeNaoExiste(conn, "Usuarios", "PermissoesCustom",     "TEXT");
        AdicionarColunaSeNaoExiste(conn, "Macros",   "Publica",              "INTEGER NOT NULL DEFAULT 0");
        AdicionarColunaSeNaoExiste(conn, "Macros",   "TotalCurtidas",        "INTEGER NOT NULL DEFAULT 0");

        AtualizarIconesLegadoParaSegoeMdl2(conn);
    }

    // Bancos criados antes da migração para ícones reais (Segoe MDL2) ainda têm emoji salvos nas categorias seed.
    private static void AtualizarIconesLegadoParaSegoeMdl2(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Categorias SET Icone='' WHERE Nome='Suporte'         AND Icone='🎧';
            UPDATE Categorias SET Icone=''      WHERE Nome='Desenvolvimento' AND Icone='💻';
            UPDATE Categorias SET Icone=''    WHERE Nome='Atendimento'      AND Icone='💬';
        ";
        cmd.ExecuteNonQuery();
    }

    private static void AdicionarColunaSeNaoExiste(SqliteConnection conn, string tabela, string coluna, string definicao)
    {
        using var check = conn.CreateCommand();
        check.CommandText = $"PRAGMA table_info({tabela});";
        using var reader = check.ExecuteReader();
        while (reader.Read())
        {
            var nomeColuna = reader.GetString(reader.GetOrdinal("name"));
            if (string.Equals(nomeColuna, coluna, StringComparison.OrdinalIgnoreCase))
                return;
        }
        reader.Close();

        using var alter = conn.CreateCommand();
        alter.CommandText = $"ALTER TABLE {tabela} ADD COLUMN {coluna} {definicao};";
        alter.ExecuteNonQuery();
    }
}
