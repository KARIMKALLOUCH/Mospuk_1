using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

public class MySqlDatabase
{
    private static string connectionString;
    private MySqlConnection connection;
    private MySqlTransaction transaction;

    public static string Server { get; set; }
    public static string Port { get; set; }
    public static string Database { get; set; }
    public static string Username { get; set; }
    public static string Password { get; set; }

    public static void Initialize(string server, string port, string database, string username, string password)
    {
        Server = server;
        Port = port;
        Database = database;
        Username = username;
        Password = password;
        connectionString = $"Server={server};Port={port};Database={database};User ID={username};Password={password};";
    }

    public MySqlDatabase()
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection information is not initialized. Call Initialize method first.");
        }

        connection = new MySqlConnection(connectionString);
        ExecuteNonQuery("SET SESSION sql_mode = (SELECT REPLACE(@@sql_mode, 'ONLY_FULL_GROUP_BY', ''));", null);
    }

    public bool OpenConnection()
    {
        try
        {
            if (connection.State == ConnectionState.Closed)
                connection.Open();
            return true;
        }
        catch (MySqlException ex)
        {
            MessageBox.Show($"Erreur de connexion à la base de données: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    public void CloseConnection()
    {
        try
        {
            if (connection.State == ConnectionState.Open)
                connection.Close();
        }
        catch (MySqlException ex)
        {
            MessageBox.Show("Erreur: " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void BeginTransaction()
    {
        if (connection == null)
            throw new InvalidOperationException("No open connection for transaction.");

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

    public bool ExecuteNonQuery(string query, List<MySqlParameter> parameters)
    {
        try
        {
            if (OpenConnection())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters.ToArray());

                    command.ExecuteNonQuery();
                }
                return true;
            }
        }
        catch (MySqlException ex)
        {
            MessageBox.Show("Erreur: " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        finally
        {
            CloseConnection();
        }

        return false;
    }

    public DataTable ExecuteQuery(string query, List<MySqlParameter> parameters)
    {
        DataTable dataTable = new DataTable();

        try
        {
            if (OpenConnection())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters.ToArray());

                    using (var adapter = new MySqlDataAdapter(command))
                        adapter.Fill(dataTable);
                }
            }
        }
        catch (MySqlException ex)
        {
            MessageBox.Show("Erreur: " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            CloseConnection();
        }

        return dataTable;
    }

    public object ExecuteScalar(string query, List<MySqlParameter> parameters)
    {
        object result = null;

        try
        {
            if (OpenConnection())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters.ToArray());

                    result = command.ExecuteScalar();
                }
            }
        }
        catch (MySqlException ex)
        {
            MessageBox.Show("Erreur: " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            CloseConnection();
        }

        return result;
    }

    public MySqlDataReader ExecuteReader(string query, List<MySqlParameter> parameters)
    {
        MySqlDataReader reader = null;

        try
        {
            if (OpenConnection())
            {
                var command = new MySqlCommand(query, connection);
                if (parameters != null)
                    command.Parameters.AddRange(parameters.ToArray());

                reader = command.ExecuteReader(CommandBehavior.CloseConnection);
            }
        }
        catch (MySqlException ex)
        {
            MessageBox.Show("Erreur: " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        return reader;
    }

    public bool ExecuteTransaction(List<string> queries, List<List<MySqlParameter>> parametersList)
    {
        if (queries.Count != parametersList.Count)
            throw new ArgumentException("The number of queries and parameter lists must match.");

        MySqlTransaction transaction = null;

        try
        {
            if (OpenConnection())
            {
                transaction = connection.BeginTransaction();

                for (int i = 0; i < queries.Count; i++)
                {
                    using (var command = new MySqlCommand(queries[i], connection, transaction))
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
        catch (MySqlException ex)
        {
            transaction?.Rollback();
            MessageBox.Show("Transaction Error: " + ex.Message);
            return false;
        }
        finally
        {
            CloseConnection();
        }

        return false;
    }
    public string GetSavedPathById(int userId, string pathType)
    {
        try
        {
            string query = "SELECT path_value FROM user_paths WHERE user_id = @userId AND path_type = @pathType";
            var parameters = new List<MySqlParameter>
        {
            new MySqlParameter("@userId", userId),
            new MySqlParameter("@pathType", pathType)
        };

            object result = ExecuteScalar(query, parameters);
            return result?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error retrieving path: " + ex.Message,
                          "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return string.Empty;
        }
    }

}
