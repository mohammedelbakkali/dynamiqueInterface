using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Npgsql;

namespace GenareteInterface
{
    public partial class Form1 : Form
    {
        private Dictionary<string, Control> columnControls; // Dictionnaire pour mapper les noms de colonnes aux contrôles

        public Form1()
        {
            InitializeComponent();
            LoadTableNames();
        }

        private void LoadTableNames()
        {
            string connectionString = "Server=localhost;Port=5432;Database=data_crud;User Id=postgres;Password=root";

            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                    SELECT table_name
                    FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_type = 'BASE TABLE'";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string tableName = reader.GetString(0);
                                comboBoxTables.Items.Add(tableName);
                            }
                        }
                    }
                }

                if (comboBoxTables.Items.Count > 0)
                {
                    comboBoxTables.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des noms de tables : " + ex.Message);
            }
        }

        private void LoadData(string tableName)
        {
            string connectionString = "Server=localhost;Port=5432;Database=data_crud;User Id=postgres;Password=root";

            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string query = $"SELECT * FROM {tableName}";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                DataTable dataTable = new DataTable();
                                dataTable.Load(reader);
                                dataGridView1.DataSource = dataTable;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des données : " + ex.Message);
            }
        }

        private void LoadChildData(string tableName)
        {
            string connectionString = "Server=localhost;Port=5432;Database=data_crud;User Id=postgres;Password=root";

            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string query = $"SELECT * FROM {tableName}";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                DataTable dataTable = new DataTable();
                                dataTable.Load(reader);
                                dataGridView2.DataSource = dataTable;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des données des enfants : " + ex.Message);
            }
        }

        private void comboBoxTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedTable = comboBoxTables.SelectedItem.ToString();
            LoadData(selectedTable);

            List<string> childTables = GetChildTables(selectedTable);
            comboBoxChildTables.Items.Clear();
            foreach (string childTable in childTables)
            {
                comboBoxChildTables.Items.Add(childTable);
            }

            if (comboBoxChildTables.Items.Count > 0)
            {
                comboBoxChildTables.SelectedIndex = 0; // Sélectionner le premier enfant par défaut
            }
            else
            {
                dataGridView2.DataSource = null; // S'il n'y a pas de tables enfants, on efface le DataGridView
            }
        }

        private void comboBoxChildTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedChildTable = comboBoxChildTables.SelectedItem.ToString();
            LoadChildData(selectedChildTable);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Vérifiez que l'utilisateur a cliqué sur une cellule
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dataGridView1.Rows[e.RowIndex];

                // Générer dynamiquement les contrôles pour afficher les données de la ligne sélectionnée
                GenerateControlsForSelectedRow(row);
            }
        }
        private List<string> GetChildTables(string tableName)
        {
            List<string> childTables = new List<string>();
            string connectionString = "Server=localhost;Port=5432;Database=data_crud;User Id=postgres;Password=root";

            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT DISTINCT
                    tc.table_name AS child_table
                FROM
                    information_schema.table_constraints AS tc
                    JOIN information_schema.key_column_usage AS kcu
                    ON tc.constraint_name = kcu.constraint_name
                    AND tc.table_schema = kcu.table_schema
                    JOIN information_schema.constraint_column_usage AS ccu
                    ON ccu.constraint_name = tc.constraint_name
                    AND ccu.table_schema = tc.table_schema
                WHERE
                    tc.constraint_type = 'FOREIGN KEY' AND
                    ccu.table_name = @parentTable";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("parentTable", tableName);

                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string childTable = reader.GetString(0);
                                childTables.Add(childTable);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des tables enfants : " + ex.Message);
            }

            return childTables;
        }


        private void GenerateControlsForSelectedRow(DataGridViewRow row)
        {
            ClearColumnControls();

            columnControls = new Dictionary<string, Control>();

            // Créer les contrôles pour chaque colonne de la ligne sélectionnée
            foreach (DataGridViewCell cell in row.Cells)
            {
                string columnName = dataGridView1.Columns[cell.ColumnIndex].HeaderText;

                // Créer un Label pour le nom de la colonne
                Label label = new Label();
                label.Text = columnName + ":";
                label.AutoSize = true;

                // Créer un TextBox ou un ComboBox pour la valeur de la colonne
                Control inputControl;
                if (IsColumnForeignKey(columnName))
                {
                    // Si c'est une clé étrangère, charger les valeurs possibles depuis la table référencée
                    ComboBox comboBox = new ComboBox();
                    comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                    LoadForeignKeyValues(columnName, comboBox);
                    comboBox.SelectedValue = cell.Value;
                    inputControl = comboBox;
                }
                else
                {
                    TextBox textBox = new TextBox();
                    textBox.Text = cell.Value.ToString();
                    inputControl = textBox;
                }

                // Ajouter les contrôles au formulaire
                flowLayoutPanel1.Controls.Add(label);
                flowLayoutPanel1.Controls.Add(inputControl);

                // Mapper le nom de la colonne au contrôle correspondant pour accès ultérieur
                columnControls[columnName] = inputControl;
            }

            // Ajouter un bouton pour enregistrer les modifications
            Button saveButton = new Button();
            saveButton.Text = "Enregistrer";
            saveButton.Click += SaveChanges_Click;
            flowLayoutPanel1.Controls.Add(saveButton);
        }

        private bool IsColumnForeignKey(string columnName)
        {
            // Vous pouvez implémenter la logique pour vérifier si la colonne est une clé étrangère
            // Par exemple, en interrogeant les métadonnées de la base de données
            return false; // Pour l'exemple, renvoyer toujours faux
        }

        private void LoadForeignKeyValues(string columnName, ComboBox comboBox)
        {
            // Implémentez le chargement des valeurs possibles pour une clé étrangère depuis la base de données
            // Utilisez columnName pour déterminer la table de référence et chargez les valeurs dans comboBox
            // Exemple simplifié : charger toutes les valeurs distinctes de la colonne de référence
            comboBox.Items.Add("Valeur1");
            comboBox.Items.Add("Valeur2");
            comboBox.Items.Add("Valeur3");
        }

        private void SaveChanges_Click(object sender, EventArgs e)
        {
            // Enregistrer les modifications de la ligne sélectionnée dans la base de données
            // Récupérer les nouvelles valeurs depuis les contrôles et exécuter la mise à jour dans la base de données

            // Exemple : récupérer les nouvelles valeurs
            foreach (var kvp in columnControls)
            {
                string columnName = kvp.Key;
                Control control = kvp.Value;

                if (control is TextBox textBox)
                {
                    // Exemple : mise à jour d'une colonne de type texte
                    string newValue = textBox.Text;
                    // Implémentez la logique pour mettre à jour la colonne dans la base de données
                }
                else if (control is ComboBox comboBox)
                {
                    // Exemple : mise à jour d'une colonne avec ComboBox (clé étrangère)
                    string newValue = comboBox.SelectedItem.ToString();
                    // Implémentez la logique pour mettre à jour la colonne dans la base de données
                }
            }

            MessageBox.Show("Modifications enregistrées avec succès !");
        }

        private void ClearColumnControls()
        {
            // Effacer tous les contrôles d'entrée dans flowLayoutPanel1
            flowLayoutPanel1.Controls.Clear();
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Gérer les clics sur dataGridView2 si nécessaire
        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
