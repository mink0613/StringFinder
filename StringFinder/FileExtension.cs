using System;
using System.IO;
using System.Windows.Forms;

namespace StringFinder
{
    public partial class FileExtension : Form
    {
        private const string _fileName = "extension.txt";

        public FileExtension()
        {
            InitializeComponent();
            ReadFile();
        }

        private void ReadFile()
        {
            if (!File.Exists(_fileName))
            {
                return;
            }

            StreamReader reader = new StreamReader(_fileName);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                ListViewItem item = new ListViewItem(line);
                lvList.Items.Add(item);
            }

            reader.Close();
        }

        private void SaveFile()
        {
            StreamWriter writer = new StreamWriter(_fileName);
            foreach (ListViewItem item in lvList.Items)
            {
                writer.WriteLine(item.Text);
            }

            writer.Flush();
            writer.Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (tbExtension.Text == null || tbExtension.Text.Length == 0)
            {
                return;
            }

            ListViewItem item = new ListViewItem(tbExtension.Text);
            lvList.Items.Add(item);

            SaveFile();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (lvList.SelectedItems == null || lvList.SelectedItems.Count == 0)
            {
                return;
            }

            foreach (ListViewItem item in lvList.SelectedItems)
            {
                lvList.Items.Remove(item);
            }

            SaveFile();
        }
    }
}
