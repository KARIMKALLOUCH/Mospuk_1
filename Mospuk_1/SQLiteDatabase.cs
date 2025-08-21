using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

public class SQLiteDatabase
{
    private string connectionString;
    private SQLiteConnection connection;
    private SQLiteTransaction transaction;

    public SQLiteDatabase(string databasePath = "database.db")
    {
        // إنشاء مجلد البيانات إذا لم يكن موجوداً
        string dataDirectory = Path.Combine(Application.StartupPath, "Data");
        if (!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
        }

        // مسار قاعدة البيانات
        string fullPath = Path.Combine(dataDirectory, databasePath);
        connectionString = $"Data Source={fullPath};Version=3;";

        // إنشاء قاعدة البيانات والجداول إذا لم تكن موجودة
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                CreateTables(conn);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في إنشاء قاعدة البيانات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CreateTables(SQLiteConnection conn)
    {
        string[] createTableQueries = {
            // جدول المستخدمين
          
            @"CREATE TABLE IF NOT EXISTS users (
            user_id INTEGER PRIMARY KEY AUTOINCREMENT,
            first_name TEXT NOT NULL,
            last_name  TEXT NOT NULL,
            user_code  TEXT NOT NULL UNIQUE,
            email      TEXT,
            phone      TEXT,
            address    TEXT,
            notes      TEXT,
            registration_date DATE DEFAULT CURRENT_DATE,
            last_update_date  DATETIME DEFAULT CURRENT_TIMESTAMP
            );",

            // جدول مسارات المستخدمين
            @"CREATE TABLE IF NOT EXISTS user_paths (
                 id INTEGER PRIMARY KEY AUTOINCREMENT,
                path_type TEXT NOT NULL UNIQUE,   -- كل نوع مسار مرة وحدة فقط
                path_value TEXT NOT NULL,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
                
            );",

            // جدول العملاء
                @"CREATE TABLE IF NOT EXISTS clients (
                    client_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    first_name TEXT NOT NULL,
                    last_name TEXT NOT NULL,
                    client_code TEXT NOT NULL UNIQUE,
                    email TEXT,
                    phone TEXT,
                    address TEXT,
                    notes TEXT,
                    registration_date DATE DEFAULT CURRENT_DATE,
                    last_update_date DATETIME DEFAULT CURRENT_TIMESTAMP
                );",

            // جدول الشركات
            @"CREATE TABLE IF NOT EXISTS companies (
                company_id INTEGER PRIMARY KEY AUTOINCREMENT,
                company_name TEXT NOT NULL,
                company_code TEXT NOT NULL UNIQUE,
                tax_number TEXT UNIQUE,
                address TEXT,
                phone TEXT,
                email TEXT,
                notes TEXT,
                registration_date DATE DEFAULT CURRENT_DATE,
                last_update_date DATETIME DEFAULT CURRENT_TIMESTAMP
            );",

            // جدول أنواع الوثائق
            @"CREATE TABLE IF NOT EXISTS document_types (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
            );",

            // جدول أزواج اللغات
            @"CREATE TABLE IF NOT EXISTS language_pairs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
            );",

            // جدول المشاريع
            @"CREATE TABLE IF NOT EXISTS projects (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                company_client TEXT NOT NULL,
                reception_date DATE NOT NULL,
                reception_time TEXT NOT NULL,
                delivery_days INTEGER NOT NULL,
                delivery_date DATE NOT NULL,
                hours_spent INTEGER DEFAULT 24,
                project_order INTEGER NOT NULL,
                folder_name TEXT NOT NULL,
                note TEXT,
                document_type TEXT NOT NULL,
                translation_type TEXT NOT NULL,
                registration_date DATE DEFAULT CURRENT_DATE,
                last_update_date DATETIME DEFAULT CURRENT_TIMESTAMP
            );",

            // جدول العناصر
            @"CREATE TABLE IF NOT EXISTS items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                project_id INTEGER NOT NULL,
                image_name TEXT NOT NULL,
                image_path TEXT NOT NULL,
                attachment_type TEXT,
                registration_date DATE DEFAULT CURRENT_DATE,
                last_update_date DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
                );",

            @"CREATE TRIGGER IF NOT EXISTS update_users_timestamp 
                AFTER UPDATE ON users
                BEGIN
                    UPDATE users SET last_update_date = CURRENT_TIMESTAMP 
                WHERE user_id = NEW.user_id;
               END;",

            // إنشاء تريجرز لتحديث التاريخ
            @"CREATE TRIGGER IF NOT EXISTS update_user_paths_timestamp 
                AFTER UPDATE ON user_paths
                BEGIN
                    UPDATE user_paths SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
                END;",

            @"CREATE TRIGGER IF NOT EXISTS update_clients_timestamp 
                AFTER UPDATE ON clients
                BEGIN
                    UPDATE clients SET last_update_date = CURRENT_TIMESTAMP WHERE client_id = NEW.client_id;
                END;",

            @"CREATE TRIGGER IF NOT EXISTS update_companies_timestamp 
                AFTER UPDATE ON companies
                BEGIN
                    UPDATE companies SET last_update_date = CURRENT_TIMESTAMP WHERE company_id = NEW.company_id;
                END;",

            @"CREATE TRIGGER IF NOT EXISTS update_document_types_timestamp 
                AFTER UPDATE ON document_types
                BEGIN
                    UPDATE document_types SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
                END;",

            @"CREATE TRIGGER IF NOT EXISTS update_language_pairs_timestamp 
                AFTER UPDATE ON language_pairs
                BEGIN
                    UPDATE language_pairs SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
                END;",

            @"CREATE TRIGGER IF NOT EXISTS update_projects_timestamp 
                AFTER UPDATE ON projects
                BEGIN
                    UPDATE projects SET last_update_date = CURRENT_TIMESTAMP WHERE id = NEW.id;
                END;",

            @"CREATE TRIGGER IF NOT EXISTS update_items_timestamp 
                AFTER UPDATE ON items
                BEGIN
                    UPDATE items SET last_update_date = CURRENT_TIMESTAMP WHERE id = NEW.id;
                END;"
        };

        foreach (string query in createTableQueries)
        {
            using (var command = new SQLiteCommand(query, conn))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    public bool OpenConnection()
    {
        try
        {
            if (connection == null)
                connection = new SQLiteConnection(connectionString);

            if (connection.State == ConnectionState.Closed)
                connection.Open();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في الاتصال بقاعدة البيانات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    public void CloseConnection()
    {
        try
        {
            if (connection?.State == ConnectionState.Open)
                connection.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void BeginTransaction()
    {
        if (connection == null)
            throw new InvalidOperationException("لا يوجد اتصال مفتوح للمعاملة.");

        if (connection.State == ConnectionState.Closed)
            connection.Open();

        transaction = connection.BeginTransaction();
    }

    public void CommitTransaction()
    {
        transaction?.Commit();
    }

    public void RollbackTransaction()
    {
        transaction?.Rollback();
    }

    public bool ExecuteNonQuery(string query, List<SQLiteParameter> parameters = null)
    {
        try
        {
            if (OpenConnection())
            {
                using (var command = new SQLiteCommand(query, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters.ToArray());

                    command.ExecuteNonQuery();
                }
                return true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        finally
        {
            CloseConnection();
        }

        return false;
    }

    public DataTable ExecuteQuery(string query, List<SQLiteParameter> parameters = null)
    {
        DataTable dataTable = new DataTable();

        try
        {
            if (OpenConnection())
            {
                using (var command = new SQLiteCommand(query, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters.ToArray());

                    using (var adapter = new SQLiteDataAdapter(command))
                        adapter.Fill(dataTable);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            CloseConnection();
        }

        return dataTable;
    }

    public object ExecuteScalar(string query, List<SQLiteParameter> parameters = null)
    {
        object result = null;

        try
        {
            if (OpenConnection())
            {
                using (var command = new SQLiteCommand(query, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters.ToArray());

                    result = command.ExecuteScalar();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            CloseConnection();
        }

        return result;
    }

    public SQLiteDataReader ExecuteReader(string query, List<SQLiteParameter> parameters = null)
    {
        SQLiteDataReader reader = null;

        try
        {
            if (OpenConnection())
            {
                var command = new SQLiteCommand(query, connection);
                if (parameters != null)
                    command.Parameters.AddRange(parameters.ToArray());

                reader = command.ExecuteReader(CommandBehavior.CloseConnection);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        return reader;
    }

    public bool ExecuteTransaction(List<string> queries, List<List<SQLiteParameter>> parametersList)
    {
        if (queries.Count != parametersList.Count)
            throw new ArgumentException("عدد الاستعلامات وقوائم المعاملات يجب أن تتطابق.");

        SQLiteTransaction transaction = null;

        try
        {
            if (OpenConnection())
            {
                transaction = connection.BeginTransaction();

                for (int i = 0; i < queries.Count; i++)
                {
                    using (var command = new SQLiteCommand(queries[i], connection, transaction))
                    {
                        if (parametersList[i] != null)
                            command.Parameters.AddRange(parametersList[i].ToArray());

                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                return true;
            }
        }
        catch (Exception ex)
        {
            transaction?.Rollback();
            MessageBox.Show("خطأ في المعاملة: " + ex.Message);
            return false;
        }
        finally
        {
            CloseConnection();
        }

        return false;
    }

    public string GetSavedPathById(string pathType)
    {
        try
        {
            const string query = "SELECT path_value FROM user_paths WHERE path_type = @pathType";
            var parameters = new List<SQLiteParameter>
        {
            new SQLiteParameter("@pathType", pathType)
        };
            object result = ExecuteScalar(query, parameters);
            return result?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في استرداد المسار: " + ex.Message,
                            "خطأ في قاعدة البيانات", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return string.Empty;
        }
    }


    // إغلاق الاتصال عند التخلص من الكائن
    public void Dispose()
    {
        transaction?.Dispose();
        connection?.Close();
        connection?.Dispose();
    }
}