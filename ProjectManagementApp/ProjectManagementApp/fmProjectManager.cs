﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectManagementApp
{
    public partial class fmProjectManager : Form
    {
        public static string m_szFile;
        public static ArrayList m_pOpenForms;

        public fmProjectManager()
        {
            try
            {
                InitializeComponent();
                StartPosition = FormStartPosition.CenterScreen;
                lvProjects.ListViewItemSorter = new CListViewComparer(CDefines.UI_LISTVIEW_PROJECTS, 0, SortOrder.Ascending);
                lvProjects.ColumnClick += LvProjects_ColumnClick;
                lvProjects.ContextMenuStrip = new CProjectRightClickMenu();
                pgProject.PropertyValueChanged += PgProject_PropertyValueChanged;
                lvProjects.DoubleClick += LvProjects_DoubleClick;

                m_pOpenForms = new ArrayList();
                string szFileName = CDefines.JSON_DEFAULT_FILE_NAME;
                if (!Properties.Settings.Default[CDefines.SETTINGS_LAST_OPENED_FILE].Equals(""))
                {
                    szFileName = (string)Properties.Settings.Default[CDefines.SETTINGS_LAST_OPENED_FILE];
                }

                CJsonDatabase.Initialize(szFileName);
                Text = $"Project Manager - [{szFileName}]";
                Properties.Settings.Default[CDefines.SETTINGS_LAST_OPENED_FILE] = szFileName;
                Properties.Settings.Default.Save();

                PopulateTypeDropdown();
                PopulateStatusDropDown();
                PopulateProjectListViewHeaders();
                PopulateProjectListView();

            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        #region "Events"
        private void LvProjects_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (lvProjects.SelectedItems.Count == 0) return;
                CListViewItem pItem = (CListViewItem)lvProjects.SelectedItems[0];
                CProject proj = (CProject)pItem.Tag;

                fmNotebook fm = new fmNotebook(proj);
                fm.Show();

                fmResources fm2 = new fmResources(proj);
                fm2.Show();

                Process.Start(proj.m_szProjectDir);

                Process.Start(proj.m_szWrikeUrl);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        private void PgProject_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            try
            {
                RefreshColumnWidths();
            }catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        private void LvProjects_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            try
            {
                CListViewComparer comparer = (CListViewComparer)lvProjects.ListViewItemSorter;
                int nColumn = e.Column;
                SortOrder pOrder = comparer.m_pSortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;

                lvProjects.ListViewItemSorter = new CListViewComparer(CDefines.UI_LISTVIEW_PROJECTS, nColumn, pOrder);

            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                string szName = tbName.Text;
                string szStatus = cbStatus.Text;
                string szType = cbType.Text;

                int nTypeID = CJsonDatabase.Instance.GetIDForLabel(CDefines.TYPE_PROJECT_TYPE, szType);
                int nStatusID = CJsonDatabase.Instance.GetIDForLabel(CDefines.TYPE_PROJECT_STATUS, szStatus);

                PopulateProjectListView(szName, nTypeID, nStatusID);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        private void lvProjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (lvProjects.SelectedItems.Count == 0) return;

                CListViewItem pSelItem = (CListViewItem)lvProjects.SelectedItems[0];
                CProject project = (CProject)pSelItem.Tag;

                pgProject.SelectedObject = project;

                if (lvProjects.ContextMenuStrip != null)
                {
                    CProjectRightClickMenu menu = (CProjectRightClickMenu)lvProjects.ContextMenuStrip;
                    menu.m_pProject = project;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        private void btnPin_Click(object sender, EventArgs e)
        {
            try
            {
                if (lvProjects.SelectedItems.Count == 0) return;
                CListViewItem pSelItem = (CListViewItem)lvProjects.SelectedItems[0];
                CProject project = (CProject)pSelItem.Tag;

                project.bPinned = !project.bPinned;

                CListViewComparer lvc = (CListViewComparer) lvProjects.ListViewItemSorter;
                lvProjects.ListViewItemSorter = null;
                lvProjects.ListViewItemSorter = lvc;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        private void btnAddProject_Click(object sender, EventArgs e)
        {
            try
            {
                CProject proj = (CProject) CJsonDatabase.Instance.Fetch(CDefines.TYPE_PROJECT, "");
                CListViewItem item = proj.CreateListViewItem(CDefines.UI_LISTVIEW_PROJECTS);

                lvProjects.Items.Add(item);
                lvProjects.SelectedItems.Clear();
                item.Selected = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        private void btnDeleteProject_Click(object sender, EventArgs e)
        {
            try
            {
                if (lvProjects.SelectedItems.Count == 0) return;
                if (DialogResult.Yes != MessageBox.Show("Are You Sure You Want To Delete This Project?", "Delete Selected Project", MessageBoxButtons.YesNo)) return;

                CListViewItem pSelItem = (CListViewItem)lvProjects.SelectedItems[0];
                CProject project = (CProject)pSelItem.Tag;

                lvProjects.Items.Remove(pSelItem);
                CJsonDatabase.Instance.Remove(project.szGuid);
                CJsonDatabase.Instance.Save(CJsonDatabase.Instance.m_szFileName);

                pgProject.SelectedObject = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        private void newFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string szFileName = "";

                SaveFileDialog dlg = new SaveFileDialog();
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                dlg.Title = "Create New File";
                dlg.DefaultExt = "json";
                dlg.Filter = "json files (*.json)|*.json";
                dlg.RestoreDirectory = true;

                if(dlg.ShowDialog() == DialogResult.OK)
                {
                    szFileName = dlg.FileName;
                } else
                {
                    return;
                }

                for (int x = m_pOpenForms.Count - 1; x >= 0; x--) ((Form)m_pOpenForms[x]).Close();

                CJsonDatabase.Initialize(szFileName);
                Text = $"Project Manager - [{szFileName}]";
                Properties.Settings.Default[CDefines.SETTINGS_LAST_OPENED_FILE] = szFileName;
                Properties.Settings.Default.Save();

                PopulateStatusDropDown();
                PopulateProjectListViewHeaders();
                PopulateProjectListView();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string szFileName = "";

                OpenFileDialog dlg = new OpenFileDialog();
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                dlg.Title = "Create New File";
                dlg.DefaultExt = "json";
                dlg.Filter = "json files (*.json)|*.json";
                dlg.RestoreDirectory = true;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    szFileName = dlg.FileName;
                }
                else
                {
                    return;
                }

                for (int x = m_pOpenForms.Count - 1; x >= 0; x--) ((Form)m_pOpenForms[x]).Close();

                CJsonDatabase.Initialize(szFileName);
                Text = $"Project Manager - [{szFileName}]";
                Properties.Settings.Default[CDefines.SETTINGS_LAST_OPENED_FILE] = szFileName;
                Properties.Settings.Default.Save();

                PopulateStatusDropDown();
                PopulateProjectListViewHeaders();
                PopulateProjectListView();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string szFileName = "";

                SaveFileDialog dlg = new SaveFileDialog();
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                dlg.Title = "Create New File";
                dlg.DefaultExt = "json";
                dlg.Filter = "json files (*.json)|*.json";
                dlg.RestoreDirectory = true;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    szFileName = dlg.FileName;
                }
                else
                {
                    return;
                }

                for (int x = m_pOpenForms.Count - 1; x >= 0; x--) ((Form)m_pOpenForms[x]).Close();

                CJsonDatabase.Instance.m_szFileName = szFileName;
                CJsonDatabase.Instance.Save(CJsonDatabase.Instance.m_szFileName);
                Text = $"Project Manager - [{szFileName}]";
                Properties.Settings.Default[CDefines.SETTINGS_LAST_OPENED_FILE] = szFileName;
                Properties.Settings.Default.Save();

                PopulateStatusDropDown();
                PopulateProjectListViewHeaders();
                PopulateProjectListView();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        private void exportToExcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (lvProjects.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please Select A Project First.");
                    return;
                }

                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.Title = "Export Project To Excel";
                saveFileDialog1.DefaultExt = "xlsx";
                saveFileDialog1.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {

                    CProject proj = (CProject)lvProjects.SelectedItems[0].Tag;
                    CNotebookPage[] lsNotes = CJsonDatabase.Instance.GetNotebookPagesFor(proj.m_szGuid);
                    string szProjNotes = "";
                    foreach (CNotebookPage pg in lsNotes) szProjNotes += $"Title: {pg.m_szName}\n{FromRtf(pg.m_szText)}\n\n";
                    szProjNotes = ToRtf(szProjNotes);
                    string szFile = saveFileDialog1.FileName;

                    Cursor = Cursors.WaitCursor;
                    lblLoadStatus.Text = "Loading...";
                    pbLoadingBar.Value = 0;
                    pbLoadingBar.Maximum = 1;

                    BackgroundWorker wkr = new BackgroundWorker();
                    wkr.DoWork += (sender2, e2) =>
                    {
                        CExporter.ToExcel(proj, szProjNotes, szFile);
                    };

                    wkr.RunWorkerCompleted += (sender2, e2) =>
                    {
                        Invoke(new MethodInvoker(() =>
                        {
                            Cursor = Cursors.Default;
                            lblLoadStatus.Text = "Done";
                            pbLoadingBar.Value = 0;
                        }));
                    };

                    wkr.RunWorkerAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        private void exportAllToExcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog ofd = new FolderBrowserDialog();
                ofd.Description = "Select Export Destination";

                if(ofd.ShowDialog() == DialogResult.OK)
                {
                    Cursor= Cursors.WaitCursor;
                    lblLoadStatus.Text = "Loading...";
                    pbLoadingBar.Value = 0;
                    pbLoadingBar.Maximum = lvProjects.Items.Count;

                    CProject[] projects = new CProject[lvProjects.Items.Count];
                    for(int x = 0; x < projects.Length; x++)
                    {
                        projects[x] = (CProject) lvProjects.Items[x].Tag;
                    }

                    BackgroundWorker wkr = new BackgroundWorker();
                    wkr.DoWork += (sender2, e2) =>
                    {
                        string szPath = ofd.SelectedPath;
                        foreach (CProject proj in projects)
                        {
                            Invoke(new MethodInvoker(() => 
                            {
                                lblLoadStatus.Text = $"Exporting \"{proj.m_szName}\"";
                                pbLoadingBar.Increment(1);
                            }));

                            int nFileNameLen = proj.m_szName.Length + 5 < 31 ? proj.m_szName.Length : 31-5;
                            string szFileName = $"{proj.m_szName.Substring(0, nFileNameLen)}.xlsx";
                            foreach (string c in new string[] { ":", "\\", "/", "?", "*", "[", "]" }) szFileName = szFileName.Replace(c, "");
                            string szFile = $"{szPath}\\{szFileName}";

                            string szNotes = "";
                            CNotebookPage[] lsNotes = CJsonDatabase.Instance.GetNotebookPagesFor(proj.m_szGuid);
                            foreach (CNotebookPage pg in lsNotes) szNotes += $"Title: {pg.m_szName}\n{FromRtf(pg.m_szText)}\n\n";
                            szNotes = ToRtf(szNotes);
                            CExporter.ToExcel(proj, szNotes, szFile);
                        }
                    };

                    wkr.RunWorkerCompleted += (sender2, e2) =>
                    {
                        Invoke(new MethodInvoker(() =>
                        {
                            Cursor = Cursors.Default;
                            lblLoadStatus.Text = "Done";
                            pbLoadingBar.Value=0;
                        }));
                    };

                    wkr.RunWorkerAsync();

                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void exportToZipToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (lvProjects.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please Select A Project First.");
                    return;
                }
                ListViewItem item = lvProjects.SelectedItems[0];
                CProject proj = (CProject)item.Tag;

                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.Title = "Export Project To Zip";
                saveFileDialog1.DefaultExt = "zip";
                saveFileDialog1.Filter = "Zip files (*.zip)|*.zip|All files (*.*)|*.*";
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string szFile = saveFileDialog1.FileName;
                    BackgroundWorker wkr = new BackgroundWorker();
                    wkr.DoWork += (sender1, ex) =>
                    {
                        CExporter.ToZip(proj, szFile, (status) =>
                        {
                            Invoke(new MethodInvoker(() => { lblLoadStatus.Text = status; }));
                        });
                    };

                    wkr.RunWorkerAsync();
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show("exportToZipToolStripMenuItem_Click");
                Debug.WriteLine(ex);
            }
        }

        private void exportAllToZipToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                CProject[] lsProjs = CJsonDatabase.Instance.GetProjects();
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.Title = "Export Project To Zip";
                saveFileDialog1.DefaultExt = "zip";
                saveFileDialog1.Filter = "Zip files (*.zip)|*.zip|All files (*.*)|*.*";
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string szFile = saveFileDialog1.FileName;
                    BackgroundWorker wkr = new BackgroundWorker();
                    wkr.DoWork += (sender1, ex) =>
                    {
                        CExporter.ToZip(lsProjs, szFile, (status) =>
                        {
                            Invoke(new MethodInvoker(() => { lblLoadStatus.Text = status; }));
                        });
                    };

                    wkr.RunWorkerAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("exportAllToZipToolStripMenuItem_Click");
                Debug.WriteLine(ex);
            }
        }
        #endregion

        #region "UI Functions"
        private void PopulateProjectListView(string name = "", int type=-1, int status=-1)
        {
            CProject[] projects = CJsonDatabase.Instance.GetProjects(name, type, status);

            lvProjects.BeginUpdate();
            lvProjects.Items.Clear();
            foreach(CProject proj in projects)
            {
                lvProjects.Items.Add(proj.CreateListViewItem(CDefines.UI_LISTVIEW_PROJECTS));
            }

            RefreshColumnWidths();
            lvProjects.EndUpdate();
        }
        // PopulateStatusDropdown
        private void PopulateStatusDropDown()
        {
            cbStatus.BeginUpdate();
            cbStatus.Items.Clear();
            cbStatus.Items.Add("Any Status");
            cbStatus.Items.AddRange(CJsonDatabase.Instance.GetProjectStatusLabels());
            cbStatus.SelectedIndex = 0;
            cbStatus.EndUpdate();
        }

        // PopulateTypeDropdown
        private void PopulateTypeDropdown()
        {
            cbType.BeginUpdate();
            cbType.Items.Clear();
            cbType.Items.Add("Any Type");
            cbType.Items.AddRange(CJsonDatabase.Instance.GetProjectTypeLabels());
            cbType.SelectedIndex = 0;
            cbType.EndUpdate();
        }
        private void PopulateProjectListViewHeaders()
        {
            lvProjects.BeginUpdate();
            lvProjects.Columns.Clear();
            CColHdr[] hdrs = CDefines.UI_COLUMNS_PROJECTS;
            lvProjects.Columns.AddRange(hdrs);
            RefreshColumnWidths();
            lvProjects.EndUpdate();
        }

        private void RefreshColumnWidths()
        {
            foreach(ColumnHeader hdr in lvProjects.Columns)
            {
                hdr.Width = -2;
            }
        }

        #endregion

        private string ToRtf(string text)
        {
            RichTextBox rtb = new RichTextBox();
            rtb.Text = text;
            string szRtb = rtb.Rtf;
            rtb.Dispose();
            return szRtb;
        }
        private string FromRtf(string rtf)
        {
            RichTextBox rtb = new RichTextBox();
            rtb.Rtf = rtf;
            string szRtb = rtb.Text;
            rtb.Dispose();
            return szRtb;
        }

        private void projectTypesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                fmLabel fm = new fmLabel(CDefines.TYPE_PROJECT_TYPE);
                fm.OnLabelsUpdated += () =>
                {
                    PopulateTypeDropdown();
                    foreach (CListViewItem item in lvProjects.Items)
                    {
                        CBaseData data = (CBaseData)item.Tag;
                        data.UpdateUI();
                    }
                };
                fm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("projectTypesToolStripMenuItem_Click");
                Debug.WriteLine(ex);
            }
        }

        private void projectStatusesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                fmLabel fm = new fmLabel(CDefines.TYPE_PROJECT_STATUS);
                fm.OnLabelsUpdated += () =>
                {
                    PopulateStatusDropDown();
                    foreach (CListViewItem item in lvProjects.Items)
                    {
                        CBaseData data = (CBaseData)item.Tag;
                        data.UpdateUI();
                    }
                };
                fm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("projectTypesToolStripMenuItem_Click");
                Debug.WriteLine(ex);
            }
        }

        private void restoreDefaultLabelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                CJsonDatabase.Instance.PopulateDefaultLabels();
                PopulateTypeDropdown();
                PopulateStatusDropDown();
                PopulateProjectListView();
                CJsonDatabase.Instance.Save(CJsonDatabase.Instance.m_szFileName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
