using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TMMSE
{
    public partial class Form1 : Form
    {
        private SaveData saveData;
        private PosterData postersData;
        public Form1()
        {
            InitializeComponent();
            button2.Enabled = false;
        }

        private List<Control> list;
        private List<Control> list2;
        private void button1_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Controls.Clear();
            button2.Enabled = false;

            LoadData();
            LoadPostersData();

            list = new List<Control>();
            list2 = new List<Control>();
            CreateSaveSettings(saveData, null, list);
            CreateSaveSettings(postersData, null, list2);

            var arr = list.Where(x => x != null).OrderBy(x => -CountAllControls(x)).ToArray();
            flowLayoutPanel1.Controls.AddRange(arr);

            var arr2 = list2.Where(x => x != null).OrderBy(x => -CountAllControls(x)).ToArray();
            flowLayoutPanel1.Controls.AddRange(arr2);

            button2.Enabled = true;
        }

        private int CountAllControls(Control control)
        {
            var count = 0;
            count += control.Controls.Count;
            if (control.Controls.Count > 0)
            {
                foreach (var obj in control.Controls)
                    count += CountAllControls(obj as Control);
            }
            return count;
        }

        private void CreateSaveSettings(object obj, Control control = null, List<Control> list = null)
        {
            if (obj == null) return;

            var checksPanel = new FlowLayoutPanel()
            {
                AutoSize = true,
                Padding = new Padding(0),
                Margin = new Padding(0),
                //                BorderStyle = BorderStyle.FixedSingle,
                FlowDirection = FlowDirection.TopDown
            };

            foreach (var field in obj.GetType().GetFields())
            {
                if (field.IsNotSerialized) continue;

                Debug.WriteLine($"{field.Name} {field.FieldType.IsPrimitive} = {field.GetValue(obj)}");
                var element = CreateElement(obj, checksPanel, field.FieldType, field.Name, new ValueRef(field, obj));
                if (control != null)
                    control.Controls.Add(element);
                if (list != null)
                    list.Add(element);
            }
            if (checksPanel.Controls.Count > 0)
            {
                if (control != null)
                    control.Controls.Add(checksPanel);
                if (list != null)
                    list.Add(checksPanel);
            }
        }

        interface IValueRef
        {
            void Set(object val);
            object Get();
        }

        class ValueRef : IValueRef
        {
            private object obj;
            private FieldInfo fi;

            public ValueRef(FieldInfo fi, object obj)
            {
                this.obj = obj;
                this.fi = fi;
            }

            public void Set(object val)
            {
                if (fi.FieldType != val.GetType() && fi.FieldType.IsPrimitive)
                {
                    fi.SetValue(obj, Convert.ChangeType(val, fi.FieldType));
                }
                else
                    fi.SetValue(obj, val);
            }

            public object Get()
            {
                return fi.GetValue(obj);
            }
        }

        class ValueListRef : IValueRef
        {
            private IValueRef obj;
            private int index;
            private IList list;

            public ValueListRef(int index, IValueRef obj)
            {
                this.obj = obj;
                this.index = index;
                list = (obj.Get() as IList);
            }

            public void Set(object val)
            {
                list[index] = val;
                obj.Set(list);
            }

            public object Get()
            {
                return list[index];
            }
        }

        private Control CreateElement(object obj, Control checks, Type type, string name, IValueRef value)
        {
            Control panel = null;
            if (type.IsEnum)
            {
                panel = new FlowLayoutPanel()
                {
                    AutoSize = true,
                    BorderStyle = BorderStyle.None,
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    FlowDirection = FlowDirection.TopDown
                };

                var lb = new Label()
                {
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    AutoSize = true,
                    Text = name
                };
                panel.Controls.Add(lb);

                var cb = new ComboBox()
                {
                    Text = value.Get().ToString()
                };

                foreach (var en in Enum.GetNames(type))
                {
                    cb.Items.Add(en);
                }

                cb.SelectionChangeCommitted += delegate (object sender, EventArgs e)
                {
                    value.Set((sender as ComboBox).SelectedIndex);
                };

                panel.Controls.Add(cb);
            }
            else if (type.IsClass)
            {
                panel = new GroupBox()
                {
                    AutoSize = true,
                    MinimumSize = new Size(120, 32),
                    Margin = new Padding(3),
                    Padding = new Padding(3),
                    Text = name
                };

                var subPanel = new FlowLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    BorderStyle = BorderStyle.None,
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    FlowDirection = FlowDirection.TopDown
                };

                if (value.Get() is IList)
                {
                    var checkBoxPanel2 = new FlowLayoutPanel()
                    {
                        AutoSize = true,
                        Padding = new Padding(0),
                        Margin = new Padding(0),
                        FlowDirection = FlowDirection.TopDown
                    };

                    var list = (value.Get() as IList);
                    for (var i = 0; i < list.Count; i++)
                    {
                        var item = list[i];

                        if (item.GetType().IsPrimitive)
                        {
                            subPanel.Controls.Add(CreateElement(obj, checkBoxPanel2, item.GetType(), $"[{i}]", new ValueListRef(i, value)));
                        }
                        else
                        {

                            var subPanel2 = new TableLayoutPanel()
                            {
                                AutoSize = true,
                                Margin = new Padding(0),
                            };
                            var lb = new Label()
                            {
                                Padding = new Padding(0, 16, 0, 0),
                                AutoSize = true,
                                Text = $"[{i}]"
                            };

                            var subPanel3 = new FlowLayoutPanel()
                            {
                                Dock = DockStyle.Fill,
                                AutoSize = true,
                                Margin = new Padding(0),
                                Padding = new Padding(0),
                                FlowDirection = FlowDirection.LeftToRight
                            };
                            CreateSaveSettings(item, subPanel3);

                            subPanel2.Controls.Add(lb, 0, 0);
                            subPanel2.Controls.Add(subPanel3, 1, 0);
                            subPanel.Controls.Add(subPanel2);
                        }
                    }
                    if (checkBoxPanel2.Controls.Count > 0)
                        subPanel.Controls.Add(checkBoxPanel2);
                }
                else
                    CreateSaveSettings(value.Get(), subPanel);

                panel.Controls.Add(subPanel);


            }
            else if (value.Get() is bool)
            {
                var cb = new CheckBox()
                {
                    Text = name,
                    Checked = Convert.ToBoolean(value.Get())
                };
                cb.CheckedChanged += delegate (object sender, EventArgs e)
                {
                    value.Set((sender as CheckBox).Checked);
                };
                checks.Controls.Add(cb);
            }
            else
            {
                panel = new FlowLayoutPanel()
                {
                    AutoSize = true,
                    BorderStyle = BorderStyle.None,
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    FlowDirection = FlowDirection.TopDown
                };

                var lb = new Label()
                {
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    AutoSize = true,
                    Text = name
                };
                panel.Controls.Add(lb);

                var tb = new TextBox()
                {
                    Text = value.Get().ToString()
                };
                tb.TextChanged += delegate (object sender, EventArgs e)
                {
                    value.Set((sender as TextBox).Text);
                };
                panel.Controls.Add(tb);
            }
            return panel;
        }

        void LoadData()
        {
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/The Mercury man"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/The Mercury man");
            }
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/The Mercury man/SaveData.dat"))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream serializationStream = File.Open(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/The Mercury man/SaveData.dat", FileMode.Open);
                SaveData data = (SaveData)formatter.Deserialize(serializationStream);
                this.saveData = data;
                serializationStream.Close();
                if (this.saveData.levelCompletionData.Count < 9)
                {
                    //this.ResetToDefaults();
                }
            }
            else
            {
                //this.ResetToDefaults();
            }
        }

        public void CreateBakFile(string filename)
        {
            var bak_filename = filename + ".bak";

            File.Delete(bak_filename);
            File.Copy(filename, bak_filename);
        }

        public void SaveData()
        {
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/The Mercury man"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/The Mercury man");
            }

            var filename = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/The Mercury man/SaveData.dat";
            CreateBakFile(filename);
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream serializationStream = File.Create(filename);
            formatter.Serialize(serializationStream, this.saveData);
            serializationStream.Close();
        }

        public void LoadPostersData()
        {
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/The Mercury man"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/The Mercury man");
            }
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/The Mercury man/Posters.dat"))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream serializationStream = File.Open(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/The Mercury man/Posters.dat", FileMode.Open);
                PosterData data = (PosterData)formatter.Deserialize(serializationStream);
                this.postersData = data;
                serializationStream.Close();
                if (this.postersData.isOpened.Count < 15)
                {
                    this.postersData.isOpened = new List<bool>();
                    for (int i = 0; i < 15; i++)
                    {
                        this.postersData.isOpened.Add(false);
                    }
                }
            }
            else if (this.postersData.isOpened.Count < 15)
            {
                this.postersData.isOpened = new List<bool>();
                for (int i = 0; i < 15; i++)
                {
                    this.postersData.isOpened.Add(false);
                }
            }
        }

        public void SavePostersData()
        {
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/The Mercury man"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/The Mercury man");
            }
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream serializationStream = File.Create(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/The Mercury man/Posters.dat");
            formatter.Serialize(serializationStream, this.postersData);
            serializationStream.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //flowLayoutPanel1.Controls.Clear();
            ////foreach (var obj in list)
            ////    flowLayoutPanel1.Controls.Remove(obj);

            //list = new List<Control>();
            //CreateSaveSettings(saveData, null, list);

            //var arr = list.Where(x => x != null).OrderBy(x => -CountAllControls(x)).ToArray();
            //flowLayoutPanel1.Controls.AddRange(arr);

            SaveData();
            SavePostersData();
            flowLayoutPanel1.Controls.Clear();
            button2.Enabled = false;

        }


    }
}
