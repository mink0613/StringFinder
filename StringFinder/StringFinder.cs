using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace StringFinder
{
    public partial class StringFinder : Form
    {
        private const string _fileName = "extension.txt";

        private List<string> _extensions;

        private bool _isSearching;

        private Thread _searchingThread;

        private List<ListViewItem> _listItems;

        public StringFinder()
        {
            InitializeComponent();

            InitExtension();
            _isSearching = false;
            _listItems = new List<ListViewItem>();

            DoubleBuffered(lvResult, true);

            lvResult.DoubleClick += LvResult_DoubleClick;
            lvResult.MouseDown += LvResult_MouseDown;
            tbString.PreviewKeyDown += PreviewKeyDownEvent;
            tbFolderPath.PreviewKeyDown += PreviewKeyDownEvent;
            tbFilter.TextChanged += FilterTextChanged;
        }

        private void PreviewKeyDownEvent(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSearch_Click(null, null);
            }
        }

        private void FilterTextChanged(object sender, EventArgs e)
        {
            if (_listItems.Count == 0 || _isSearching)
            {
                return;
            }

            lvResult.Items.Clear();
            foreach (var item in _listItems)
            {
                if (item.SubItems[0].Text.Contains(tbFilter.Text))
                {
                    lvResult.Items.Add(item);
                }
            }

            UpdateTotalCountString();
        }

        private void DoubleBuffered(Control control, bool enable)
        {
            var doubleBufferPropertyInfo = control.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            doubleBufferPropertyInfo.SetValue(control, enable, null);
        }

        private void InitExtension()
        {
            _extensions = new List<string>();

            if (!File.Exists(_fileName))
            {
                ShowFileExtensionWindow();
                return;
            }

            StreamReader reader = new StreamReader(_fileName);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                _extensions.Add(line);
            }
        }

        private void LvResult_DoubleClick(object sender, EventArgs e)
        {
            var item = lvResult.SelectedItems[0];
            string filePath = item.SubItems[0].Text;

            string folderPath = System.IO.Path.GetDirectoryName(filePath);
            Process.Start(filePath);
        }

        private void LvResult_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Point mousePosition = new Point(e.X, e.Y);

                bool isMatch = false;
                foreach (ListViewItem item in lvResult.Items)
                {
                    int index = lvResult.Items.IndexOf(item);
                    lvResult.Items[index].Selected = false;

                    if (item.Bounds.Contains(mousePosition))
                    {
                        lvResult.Items[index].Selected = true;

                        string filePath = item.SubItems[0].Text;
                        string folderPath = System.IO.Path.GetDirectoryName(filePath);

                        ContextMenuStrip mi = new ContextMenuStrip();
                        mi.Items.Add(folderPath);
                        lvResult.ContextMenuStrip = mi;

                        isMatch = true;
                    }
                }

                if (isMatch)
                {
                    lvResult.ContextMenuStrip.Click += (s, e1) =>
                    {
                        ContextMenuStrip clickedItem = s as ContextMenuStrip;
                        Process.Start(clickedItem.Items[0].Text);
                    };
                    lvResult.ContextMenuStrip.Show(lvResult, new Point(e.X, e.Y));
                }
            }
        }

        private void ChangeSearchButtonText(bool isSearch)
        {
            if (isSearch == true)
            {
                btnSearch.Invoke(new MethodInvoker(delegate
                {
                    btnSearch.Text = "Stop";
                }));
            }
            else
            {
                btnSearch.Invoke(new MethodInvoker(delegate
                {
                    btnSearch.Text = "Search";
                }));
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (_isSearching == true)
            {
                _searchingThread.Abort();
                _searchingThread = null;
                _isSearching = false;
                ChangeSearchButtonText(_isSearching);
                return;
            }

            string folderPath = tbFolderPath.Text;
            if (folderPath == null || folderPath.Length == 0)
            {
                MessageBox.Show("Please enter folder path");
                return;
            }

            string stringToFind = tbString.Text;
            if (stringToFind == null || stringToFind.Length == 0)
            {
                MessageBox.Show("Please enter string to find");
                return;
            }

            lvResult.Items.Clear();
            UpdateTotalCountString();

            _isSearching = true;
            ChangeSearchButtonText(_isSearching);

            _searchingThread = new Thread(() =>
            {
                bool caseSensitive = cbCase.Checked;

                //lbSearching.Visible = true;

                Find(folderPath, stringToFind, caseSensitive);

                //lbSearching.Visible = false;

                CopyMasterListItem();

                MessageBox.Show("Finished");
                _searchingThread = null;
                _isSearching = false;
                ChangeSearchButtonText(_isSearching);
            });
            _searchingThread.Start();
        }

        private void CopyMasterListItem()
        {
            _listItems.Clear();
            this.Invoke(new Action(delegate ()
            {
                foreach (ListViewItem item in lvResult.Items)
                {
                    _listItems.Add(item);
                }
            }));
        }

        private void UpdateTotalCountString()
        {
            this.Invoke(new Action(delegate ()
            {
                lbTotalCount.Text = lvResult.Items.Count.ToString();
            }));
        }

        private void Find(string folderPath, string text, bool caseSensitive)
        {
            string[] subFolders;
            try
            {
                subFolders = Directory.GetDirectories(folderPath);
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.ToString());
                return;
            }

            if (subFolders != null && subFolders.Length > 0)
            {
                foreach (string subFolder in subFolders)
                {
                    Find(subFolder, text, caseSensitive);
                }
            }

            string[] files = Directory.GetFiles(folderPath);
            if (files == null || files.Length == 0)
            {
                return;
            }

            List<string> extensions = _extensions;
            bool isExtensionOnly = false;

            // If text contains * (ex: *.txt or *.log)
            // then it searches all files that only has specific extension
            if (text.Contains("*"))
            {
                extensions.Clear();
                extensions.Add(text.Split('.')[1]);
                isExtensionOnly = true;
            }

            if (!caseSensitive)
            {
                for (int i = 0; i < extensions.Count; i++)
                {
                    string ex = extensions[i];
                    ex = ex.ToUpperInvariant();
                    extensions.RemoveAt(i);
                    extensions.Insert(i, ex);
                }
            }

            foreach (string tempFile in files)
            {
                string file = tempFile;

                if (!caseSensitive)
                {
                    text = text.ToUpperInvariant();
                    file = file.ToUpperInvariant();
                }

                if (!isExtensionOnly && file.Contains(text))
                {
                    ListViewItem item = new ListViewItem(tempFile);

                    this.Invoke(new Action(delegate ()
                    {
                        lvResult.BeginUpdate();
                        lvResult.Items.Add(item);
                        lvResult.EndUpdate();
                    }));

                    UpdateTotalCountString();
                }
                else
                {
                    string extension = System.IO.Path.GetExtension(file);
                    if (extension.Length > 1)
                    {
                        extension = extension.Substring(1);
                    }

                    if (!extensions.Contains(extension))
                    {
                        continue;
                    }

                    bool isFound = false;
                    if (isExtensionOnly)
                    {
                        isFound = true;
                    }
                    else
                    {
                        try
                        {
                            using (FileStream fs = File.OpenRead(file))
                            using (BufferedStream bs = new BufferedStream(fs))
                            using (StreamReader sr = new StreamReader(bs))
                            {
                                string line;
                                while ((line = sr.ReadLine()) != null)
                                {
                                    if (!caseSensitive)
                                    {
                                        line = line.ToUpperInvariant();
                                    }

                                    if (line.Contains(text))
                                    {
                                        isFound = true;
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {

                        }
                    }

                    if (isFound)
                    {
                        ListViewItem item = new ListViewItem(tempFile);

                        this.Invoke(new Action(delegate ()
                        {
                            lvResult.BeginUpdate();
                            lvResult.Items.Add(item);
                            lvResult.EndUpdate();
                        }));

                        UpdateTotalCountString();
                    }
                }
            }
        }

        private void ShowFileExtensionWindow()
        {
            FileExtension fileExtension = new FileExtension();
            fileExtension.FormClosed += (s, e) =>
            {
                InitExtension();
            };
            fileExtension.Show();
        }

        private void fileExtensionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFileExtensionWindow();
        }
    }
}
