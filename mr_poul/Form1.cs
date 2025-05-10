using Npgsql;
using System;
using System.Windows.Forms;
using mr_poul;

namespace mr_poul
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Получаем логин и пароль из текстовых полей
            string name = textBox1.Text;
            string password = textBox2.Text;

            // Подключаемся к PostgreSQL
            using (var conn = new NpgsqlConnection("Host=localhost;Port=5432;Database=UP-1;Username=postgres;Password=4825;"))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    // Подстановка значений напрямую в запрос
                    cmd.CommandText = "SELECT COUNT(*) FROM users WHERE login = '" + name + "' AND password = '" + password + "'";

                    // Проверяем наличие пользователя в базе
                    int count = 0;
                    MessageBox.Show("Вход успешен!");
                    Form form2 = new Form2();
                    this.Hide();
                    form2.Show();

                    // Обрабатываем результат запроса

                    if (count > 0)
                    {

                    }
                    else
                    {
                        MessageBox.Show("Неверное имя пользователя или пароль.");
                    }
                }
                conn.Close();
            }
        }
    }
}