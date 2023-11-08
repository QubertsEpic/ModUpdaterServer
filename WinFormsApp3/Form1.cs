namespace WinFormsApp3
{
    public partial class Form1 : Form
    {
        NotifyIcon icon;

        public Form1()
        {
            InitializeComponent();
            icon = new NotifyIcon();
            var menu = new System.Windows.Forms.ContextMenuStrip();
            var items = new ToolStripMenuItem[1];
            items[0] = new ToolStripMenuItem("Exit");
            items[0].Name = "1";
            menu.Items.AddRange(items);

            icon.Text = "Hello";
            icon.Icon = new System.Drawing.Icon("C:\\Users\\matth\\Downloads\\ACLib\\playback.ico");
            icon.ContextMenuStrip = menu;

            icon.Visible = true;

            menu.ItemClicked += Menu_ItemClicked;
            icon.DoubleClick += Icon_DoubleClick;
        }

        private void Menu_ItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem?.Name != null)
                if (e.ClickedItem.Name == "1")
                    this.Close();
        }

        private void Icon_DoubleClick(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}