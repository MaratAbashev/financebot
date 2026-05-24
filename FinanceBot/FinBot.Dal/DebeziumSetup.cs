using Npgsql;

namespace FinBot.Dal;

public static class DebeziumSetup
{
    public static async Task SetupDebeziumPrivileges(string connectionString)
    {
        var sql = @"
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'debezium') THEN
                    CREATE ROLE debezium WITH LOGIN PASSWORD 'debezium_pass' REPLICATION;
                END IF;
            END
            $$;

            DO $$
            DECLARE
                db_name text;
            BEGIN
                SELECT current_database() INTO db_name;
                
                EXECUTE format('GRANT CONNECT ON DATABASE %I TO debezium', db_name);
            END
            $$;

            GRANT USAGE ON SCHEMA public TO debezium;
            
            GRANT SELECT ON ALL TABLES IN SCHEMA public TO debezium;
            
            ALTER DEFAULT PRIVILEGES IN SCHEMA public 
                GRANT SELECT ON TABLES TO debezium;

            DROP PUBLICATION IF EXISTS dbz_publication;
            CREATE PUBLICATION dbz_publication FOR ALL TABLES;
            
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_publication WHERE pubname = 'dbz_publication') THEN
                    RAISE EXCEPTION 'Publication dbz_publication was not created';
                END IF;
            END
            $$;
            
            DO $$
            DECLARE
                r RECORD;
            BEGIN
                FOR r IN 
                    SELECT schemaname, tablename 
                    FROM pg_tables 
                    WHERE schemaname = 'public'
                LOOP
                    EXECUTE format('ALTER TABLE %I.%I REPLICA IDENTITY FULL', r.schemaname, r.tablename);
                END LOOP;
            END $$;
        ";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}