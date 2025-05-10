using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace mr_poul
{
    public partial class Form2 : Form
    {
        // Объявляем переменные для работы с базой
        NpgsqlConnection conn;
        NpgsqlDataAdapter adapter;
        DataTable dt;
        // словарь для хранения данных для отображения в comboBox
        Dictionary<string, Dictionary<int, string>> lookupData = new Dictionary<string, Dictionary<int, string>>();

        public Form2()
        {
            InitializeComponent();
            string connection = "Host=localhost;Port=5432;Database=UP-1;Username=postgres;Password=4825;";
            conn = new NpgsqlConnection(connection);
            // добавляем названия таблиц в comboBox, чтобы пользователь мог выбрать
            comboBox1.Items.AddRange(new object[] { "книги", "читатели", "выдачи", "сотрудники", "users" });
            // обработчик смены выбранной таблицы
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
        }

        // Когда пользователь меняет выбор в comboBox
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string table = comboBox1.Text; // получаем выбранную таблицу

            LoadLookupTables(); // загружаем связанные таблицы для отображения

            // создаем запрос для получения всех данных из выбранной таблицы
            string query = $"SELECT * FROM {table}";
            adapter = new NpgsqlDataAdapter(query, conn);
            var builder = new NpgsqlCommandBuilder(adapter);

            dt = new DataTable();
            adapter.Fill(dt);
            dataGridView1.Columns.Clear();
            dataGridView1.DataSource = dt;
            HidePrimaryKey(table);
            ReplaceForeignKeyColumns(table);
            dataGridView1.DefaultValuesNeeded -= dataGridView1_DefaultValuesNeeded;
            dataGridView1.DefaultValuesNeeded += dataGridView1_DefaultValuesNeeded;
        }

        // Кнопка "Сохранить" — сохраняет изменения в базу
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                adapter.Update(dt);
                MessageBox.Show("Данные успешно сохранены");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
        }

        // Кнопка "Удалить" — удаляет выбранную строку
        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow != null && !dataGridView1.CurrentRow.IsNewRow)
            {
                dataGridView1.Rows.Remove(dataGridView1.CurrentRow);
                MessageBox.Show("Строка удалена. Сохраните изменения для обновления базы данных.");
            }
        }

        // Загружает связанные таблицы для отображения в ComboBox
        private void LoadLookupTables()
        {
            // загружаем словари для таблиц, связанных с FK
            lookupData["книги"] = LoadLookup("книги", "id_книги", "название");
            lookupData["читатели"] = LoadLookup("читатели", "id_читателя", "полное_имя");
        }

        // Общий метод для загрузки данных из таблицы, возвращает словарь {ключ, значение}
        private Dictionary<int, string> LoadLookup(string table, string keyCol, string valCol)
        {
            var dict = new Dictionary<int, string>();
            using (var cmd = new NpgsqlCommand($"SELECT {keyCol}, {valCol} FROM {table}", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dict[reader.GetInt32(0)] = reader.GetString(1);
                    }
                }
                conn.Close();
            }
            return dict;
        }

        // Скрывает первичный ключ, чтобы пользователь его не редактировал
        private void HidePrimaryKey(string table)
        {
            string pkColumn = null;

            // определяем название PK в зависимости от таблицы
            switch (table)
            {
                case "книги":
                    pkColumn = "id_книги";
                    break;
                case "читатели":
                    pkColumn = "id_читателя";
                    break;
                case "выдачи":
                    pkColumn = "id_выдачи";
                    break;
                case "сотрудники":
                    pkColumn = "id_сотрудника";
                    break;
                case "users":
                    pkColumn = "users_id";
                    break;
            }

            if (pkColumn != null && dataGridView1.Columns.Contains(pkColumn))
            {
                dataGridView1.Columns[pkColumn].Visible = false;
            }
        }

        // Заменяет FK-колонки на ComboBox для удобного выбора
        private void ReplaceForeignKeyColumns(string table)
        {
            switch (table)
            {
                case "выдачи":
                    ReplaceWithComboBox("id_читателя", "читатели");
                    ReplaceWithComboBox("id_книги", "книги");
                    break;
            }
        }

        // Создает и вставляет ComboBox вместо FK-колонки
        private void ReplaceWithComboBox(string columnName, string lookupTable)
        {
            if (!dt.Columns.Contains(columnName))
                return;

            if (dataGridView1.Columns.Contains(columnName))
                dataGridView1.Columns.Remove(columnName);

            var combo = new DataGridViewComboBoxColumn
            {
                Name = columnName,
                DataPropertyName = columnName,
                HeaderText = columnName.Replace("id_", ""),
                DataSource = new BindingSource(lookupData[lookupTable], null),
                DisplayMember = "Value",
                ValueMember = "Key",
                FlatStyle = FlatStyle.Flat
            };

            int index = dataGridView1.Columns.Count > 0 ? dataGridView1.Columns[columnName]?.Index ?? 0 : 0;
            dataGridView1.Columns.Insert(index, combo);
        }

        // Устанавливает значения по умолчанию при добавлении новой строки
        private void dataGridView1_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            string table = comboBox1.Text;
            switch (table)
            {
                case "выдачи":
                    SetDefaultValue(e.Row, "id_читателя", "читатели");
                    SetDefaultValue(e.Row, "id_книги", "книги");
                    e.Row.Cells["количество"].Value = 1; // по умолчанию 1
                    e.Row.Cells["дата_выдачи"].Value = DateTime.Now; // текущая дата
                    break;
                case "книги":
                    e.Row.Cells["доступное_количество"].Value = 0;
                    e.Row.Cells["общее_количество"].Value = 0;
                    break;
            }
        }

        // Устанавливает значение по умолчанию, выбирая первый ключ из словаря
        private void SetDefaultValue(DataGridViewRow row, string column, string lookupTable)
        {
            if (lookupData.ContainsKey(lookupTable) && lookupData[lookupTable].Any())
            {
                row.Cells[column].Value = lookupData[lookupTable].Keys.First();
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // Обработчик клика по pictureBox
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            // Инициализация при загрузке формы
        }
    }
}