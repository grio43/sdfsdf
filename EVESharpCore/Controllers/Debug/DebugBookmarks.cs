using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESharpCore.Controllers.Debug
{
    public partial class DebugBookmarks : Form
    {
        #region Constructors

        public DebugBookmarks()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        private void button2_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    if (waitUntil > DateTime.UtcNow)
                    {
                        action.QueueAction();
                        return;
                    }

                    var folders = ESCache.Instance.DirectEve.BookmarkFolders;
                    var res = folders.Select(n => new { n.Name, n.Id, n.OwnerId, n.CreatorId, n.IsActive, src = n }).ToList();
                    Log.WriteLine($"{folders.Count()} folder(s) found.");

                    Task.Run(() =>
                    {
                        return Invoke(new Action(() =>
                        {
                            dataGridView1.DataSource = res;
                            ModifyButtons(true);
                        }));
                    });
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            dynamic obj = null;
            try
            {
                obj = dataGridView1.CurrentRow.DataBoundItem;
            }
            catch (Exception exception)
            {
                Log.WriteLine(exception.ToString());
            }

            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    if (waitUntil > DateTime.UtcNow)
                    {
                        action.QueueAction();
                        return;
                    }



                    //PyBookmark = pyBookmark;
                    //BookmarkId = (long?)pyBookmark.Attribute("bookmarkID");
                    //CreatedOn = (DateTime?)pyBookmark.Attribute("created");
                    //ItemId = (long?)pyBookmark.Attribute("itemID");
                    //LocationId = (long?)pyBookmark.Attribute("locationID");
                    //FolderId = (long?)pyBookmark.Attribute("folderID");
                    //Title = (string)pyBookmark.Attribute("memo");
                    //if (!String.IsNullOrEmpty(Title) && Title.Contains("\t"))
                    //{
                    //    Memo = Title.Substring(Title.IndexOf("\t") + 1);
                    //    Title = Title.Substring(0, Title.IndexOf("\t"));
                    //}

                    //Note = (string)pyBookmark.Attribute("note");
                    //OwnerId = (int?)pyBookmark.Attribute("ownerID");
                    //TypeId = (int)pyBookmark.Attribute("typeID");
                    //X = (double?)pyBookmark.Attribute("x");
                    //Y = (double?)pyBookmark.Attribute("y");
                    //Z = (double?)pyBookmark.Attribute("z");

                    var bms = ESCache.Instance.DirectEve.Bookmarks.Where(c => c.FolderId.Equals(obj.Id)).ToList();
                    var res = bms.OrderBy(m => m.Title).Select(m => new { m.Title, m.BookmarkId, m.CreatedOn, m.ItemId, m.LocationId, m.FolderId, m.Note, m.OwnerId, m.TypeId, m.BookmarkType, m.X, m.Y, m.Z, src = m }).ToList();

                    Task.Run(() =>
                    {
                        return Invoke(new Action(() =>
                        {
                            dataGridView2.DataSource = res;
                            ModifyButtons(true);
                        }));
                    });
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }

        private void DebugChat_Load(object sender, EventArgs e)
        {
        }

        private void ModifyButtons(bool enabled = false)
        {
            Invoke(new Action(() =>
            {
                foreach (var b in Controls)
                    if (b is Button button)
                        button.Enabled = enabled;
            }));
        }

        #endregion Methods

        private void button1_Click(object sender, EventArgs e)
        {
            dynamic obj = null;
            try
            {
                obj = dataGridView1.CurrentRow.DataBoundItem;
            }
            catch (Exception exception)
            {
                Log.WriteLine(exception.ToString());
            }

            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    if (waitUntil > DateTime.UtcNow)
                    {
                        action.QueueAction();
                        return;
                    }

                    var name = ESCache.Instance.DirectEve?.Me?.CurrentSolarSystem?.Name;
                    if (name == null)
                        name = new Random().Next(10, 99).ToString();
                    else
                        name = $"spot in {name} solar system";

                    ESCache.Instance.DirectEve.BookmarkCurrentLocation(null, obj.Id);

                    Task.Run(() =>
                    {
                        return Invoke(new Action(() =>
                        {
                            dataGridView1_SelectionChanged(null, new EventArgs());
                            ModifyButtons(true);
                        }));
                    });
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }

        private void deleteBookmarkToolStripMenuItem_Click(object sender, EventArgs e)
        {

            dynamic obj = null;
            try
            {
                obj = dataGridView2.CurrentRow.DataBoundItem;
            }
            catch (Exception exception)
            {
                Log.WriteLine(exception.ToString());
            }

            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    if (waitUntil > DateTime.UtcNow)
                    {
                        action.QueueAction();
                        return;
                    }

                    var bm = ESCache.Instance.DirectEve.Bookmarks.FirstOrDefault(e => e.BookmarkId == obj.BookmarkId);

                    if (bm != null)
                        bm.Delete();

                    Task.Run(() =>
                    {
                        return Invoke(new Action(() =>
                        {
                            dataGridView1_SelectionChanged(null, new EventArgs());
                            ModifyButtons(true);
                        }));
                    });
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }
    }
}