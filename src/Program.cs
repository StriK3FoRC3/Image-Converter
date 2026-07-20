using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using ImageMagick;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

[assembly: AssemblyTitle("Image Converter")]
[assembly: AssemblyDescription("Portable Windows batch image and video converter")]
[assembly: AssemblyCompany("StriK3FoRC3")]
[assembly: AssemblyProduct("Image Converter")]
[assembly: AssemblyCopyright("Copyright (c) 2026 StriK3FoRC3")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace ImageConverterUtility
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Any(arg => string.Equals(arg, "--batch", StringComparison.OrdinalIgnoreCase)))
            {
                using (ConverterForm form = new ConverterForm())
                {
                    return form.RunBatchForCommandLine();
                }
            }
            Application.Run(new ConverterForm());
            return 0;
        }
    }

    internal sealed class ConverterForm : Form
    {
        private static readonly Color ColorBg = Color.FromArgb(25, 25, 25);
        private static readonly Color ColorPanel = Color.FromArgb(35, 35, 35);
        private static readonly Color ColorTab = Color.FromArgb(43, 43, 43);
        private static readonly Color ColorText = Color.FromArgb(245, 245, 245);
        private static readonly Color ColorMuted = Color.FromArgb(180, 180, 180);
        private static readonly Color ColorAccent = Color.FromArgb(156, 99, 255);
        private static readonly Color ColorBlue = Color.FromArgb(82, 139, 255);
        private static readonly Color ColorGreen = Color.FromArgb(76, 175, 80);
        private static readonly Color ColorRed = Color.FromArgb(244, 67, 54);
        private static readonly Color ColorYellow = Color.FromArgb(255, 214, 64);

        private readonly ListView fileList = new ListView();
        private readonly ComboBox outputFormatBox = new ComboBox();
        private readonly TextBox widthBox = new TextBox();
        private readonly TextBox heightBox = new TextBox();
        private readonly TextBox scaleBox = new TextBox();
        private readonly Label statusLabel = new Label();
        private readonly StyledButton refreshButton;
        private readonly StyledButton openInputButton;
        private readonly StyledButton openOutputButton;
        private readonly StyledButton convertButton;
        private readonly StyledButton clearButton;
        private readonly StyledButton cancelButton;
        private readonly StyledButton infoButton;
        private CancellationTokenSource conversionCancel;
        private FormatPopup formatPopup;
        private bool fittingColumns;
        private bool uiBuilt;

        private readonly Font titleFont = new Font("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Pixel);
        private readonly Font normalFont = new Font("Segoe UI", 14f, FontStyle.Regular, GraphicsUnit.Pixel);
        private readonly Font buttonFont = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Pixel);

        private readonly string exeDir;
        private readonly string inputDir;
        private readonly string outputDir;
        private readonly HashSet<string> imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".avif", ".bmp", ".cur", ".dds", ".dpx", ".exr", ".fits", ".gif", ".hdr", ".heic",
            ".heif", ".icb", ".ico", ".j2c", ".j2k", ".jng", ".jp2", ".jpc", ".jpeg", ".jpg",
            ".jxl", ".mng", ".pam", ".pbm", ".pcx", ".pdf", ".pgm", ".png", ".pnm", ".ppm", ".psd",
            ".qoi", ".svg", ".svgz", ".tga", ".tif", ".tiff", ".vda", ".vst", ".wbmp",
            ".webp", ".xbm", ".xpm"
        };
        private readonly HashSet<string> videoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".avi", ".mkv", ".mov", ".mp4", ".webm"
        };
        private readonly List<OutputFormat> outputFormats = BuildOutputFormats();

        private static List<OutputFormat> BuildOutputFormats()
        {
            return new List<OutputFormat>
            {
                new OutputFormat("PNG", ".png", MagickFormat.Png, false),
                new OutputFormat("JPEG", ".jpg", MagickFormat.Jpeg, false, ".jpeg"),
                new OutputFormat("WebP", ".webp", MagickFormat.WebP, true),
                new OutputFormat("GIF", ".gif", MagickFormat.Gif, true),
                new OutputFormat("BMP", ".bmp", MagickFormat.Bmp, false),
                new OutputFormat("TIFF", ".tif", MagickFormat.Tiff, false, ".tiff"),
                new OutputFormat("ICO", ".ico", MagickFormat.Ico, false, 256),
                new OutputFormat("SVG", ".svg", MagickFormat.Svg, false),
                new OutputFormat("AVIF", ".avif", MagickFormat.Avif, false),
                new OutputFormat("JPEG XL", ".jxl", MagickFormat.Jxl, false),
                new OutputFormat("JPEG 2000", ".jp2", MagickFormat.Jp2, false, ".j2k", ".j2c", ".jpc"),
                new OutputFormat("SVGZ", ".svgz", MagickFormat.Svgz, false),
                new OutputFormat("CUR", ".cur", MagickFormat.Cur, false, 256),
                new OutputFormat("DDS", ".dds", MagickFormat.Dds, false),
                new OutputFormat("EXR", ".exr", MagickFormat.Exr, false),
                new OutputFormat("HDR", ".hdr", MagickFormat.Hdr, false),
                new OutputFormat("TGA", ".tga", MagickFormat.Tga, false, ".icb", ".vda", ".vst"),
                new OutputFormat("PCX", ".pcx", MagickFormat.Pcx, false),
                new OutputFormat("PSD", ".psd", MagickFormat.Psd, false),
                new OutputFormat("QOI", ".qoi", MagickFormat.Qoi, false),
                new OutputFormat("PNM", ".pnm", MagickFormat.Pnm, false),
                new OutputFormat("PBM", ".pbm", MagickFormat.Pbm, false),
                new OutputFormat("PGM", ".pgm", MagickFormat.Pgm, false),
                new OutputFormat("PPM", ".ppm", MagickFormat.Ppm, false),
                new OutputFormat("PAM", ".pam", MagickFormat.Pam, false),
                new OutputFormat("WBMP", ".wbmp", MagickFormat.Wbmp, false),
                new OutputFormat("XBM", ".xbm", MagickFormat.Xbm, false),
                new OutputFormat("XPM", ".xpm", MagickFormat.Xpm, false),
                new OutputFormat("FITS", ".fits", MagickFormat.Fits, false),
                new OutputFormat("DPX", ".dpx", MagickFormat.Dpx, false),
                new OutputFormat("JNG", ".jng", MagickFormat.Jng, false),
                new OutputFormat("MNG", ".mng", MagickFormat.Mng, true)
            };
        }

        public ConverterForm()
        {
            exeDir = AppDomain.CurrentDomain.BaseDirectory;
            inputDir = Path.Combine(exeDir, "Input");
            outputDir = Path.Combine(exeDir, "Output");
            Directory.CreateDirectory(inputDir);
            Directory.CreateDirectory(outputDir);

            refreshButton = new StyledButton("Refresh", ColorBlue, buttonFont);
            openInputButton = new StyledButton("Input", ColorAccent, buttonFont);
            openOutputButton = new StyledButton("Output", ColorAccent, buttonFont);
            convertButton = new StyledButton("Convert", ColorGreen, buttonFont);
            clearButton = new StyledButton("Clear", ColorRed, buttonFont);
            cancelButton = new StyledButton("Cancel", ColorRed, buttonFont);
            infoButton = new StyledButton("i", ColorYellow, new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Pixel));

            Text = "Image Converter";
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            MinimumSize = new System.Drawing.Size(624, 432);
            Size = new System.Drawing.Size(720, 496);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ColorBg;
            ForeColor = ColorText;
            Font = normalFont;
            AllowDrop = true;

            SetDarkTitleBar();
            BuildUi();
            WireEvents();
            RefreshFileList();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SetDarkTitleBar();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutControls();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            DrawPurpleOutline(e.Graphics, widthBox.Bounds, 6);
            DrawPurpleOutline(e.Graphics, heightBox.Bounds, 6);
            DrawPurpleOutline(e.Graphics, scaleBox.Bounds, 6);
            Rectangle listOutline = new Rectangle(fileList.Left, fileList.Top - 5, fileList.Width, fileList.Height + 5);
            DrawPurpleOutline(e.Graphics, listOutline, 6);
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            base.OnDragEnter(e);
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
            int copied = 0;
            int duplicates = 0;
            foreach (string path in ExpandDroppedFiles(dropped))
            {
                string ext = Path.GetExtension(path);
                if (!imageExtensions.Contains(ext) && !videoExtensions.Contains(ext)) continue;
                if (IsDuplicateInput(path))
                {
                    duplicates++;
                    continue;
                }
                string target = Path.Combine(inputDir, Path.GetFileName(path));
                File.Copy(path, target);
                copied++;
            }
            RefreshFileList();
            string message = copied > 0 ? "Added " + copied + " file(s) to Input." : (duplicates > 0 ? "Duplicate file(s) skipped." : "No supported files found in the drop.");
            SetStatus(message, copied > 0 ? ColorGreen : ColorYellow);
            base.OnDragDrop(e);
        }

        private void BuildUi()
        {
            Controls.Add(MakeLabel("Image Converter", titleFont, ColorText));
            Controls.Add(MakeLabel("Batch converts every supported file in Input. Drop files here to add them.", normalFont, ColorMuted));

            ConfigureCombo(outputFormatBox, outputFormats.Cast<object>(), LoadSelectedFormatIndex());
            ConfigureNumberText(widthBox, "");
            ConfigureNumberText(heightBox, "");
            ConfigureNumberText(scaleBox, "100");
            ConfigureScaleBox();

            fileList.View = View.Details;
            fileList.FullRowSelect = true;
            fileList.MultiSelect = true;
            fileList.HideSelection = false;
            fileList.BorderStyle = BorderStyle.None;
            fileList.BackColor = ColorPanel;
            fileList.ForeColor = ColorText;
            fileList.Font = normalFont;
            fileList.OwnerDraw = true;
            fileList.Scrollable = true;
            fileList.Columns.Add("File", 130);
            fileList.Columns.Add("Type", 70);
            fileList.Columns.Add("Size", 85);
            fileList.Columns.Add("Status", 320);
            fileList.DrawColumnHeader += DrawListHeader;
            fileList.DrawItem += delegate { };
            fileList.DrawSubItem += DrawListSubItem;
            fileList.ColumnWidthChanged += delegate { FitColumnsToList(); };
            fileList.ColumnWidthChanging += delegate(object sender, ColumnWidthChangingEventArgs e)
            {
                e.NewWidth = fileList.Columns[e.ColumnIndex].Width;
                e.Cancel = true;
                HideHorizontalScrollBar();
            };

            statusLabel.Font = normalFont;
            statusLabel.ForeColor = ColorMuted;
            statusLabel.BackColor = ColorBg;
            statusLabel.AutoEllipsis = true;
            statusLabel.Text = "Ready.";

            Controls.Add(outputFormatBox);
            Controls.Add(widthBox);
            Controls.Add(heightBox);
            Controls.Add(scaleBox);
            Controls.Add(refreshButton);
            Controls.Add(openInputButton);
            Controls.Add(openOutputButton);
            Controls.Add(convertButton);
            Controls.Add(clearButton);
            Controls.Add(cancelButton);
            Controls.Add(infoButton);
            Controls.Add(fileList);
            Controls.Add(statusLabel);

            uiBuilt = true;
            LayoutControls();
        }

        private void WireEvents()
        {
            refreshButton.Click += delegate { RefreshFileList(); };
            openInputButton.Click += delegate { OpenFolder(inputDir); };
            openOutputButton.Click += delegate { OpenFolder(outputDir); };
            convertButton.Click += async delegate { await ConvertAllAsync(); };
            clearButton.Click += delegate { ClearInput(); };
            outputFormatBox.SelectedIndexChanged += delegate
            {
                SaveSelectedFormat();
                ShowSelectedFormatWarning();
            };
            cancelButton.Click += delegate
            {
                if (conversionCancel != null) conversionCancel.Cancel();
            };
            infoButton.Click += delegate { ShowSupportedFormatsDialog(); };
        }

        private void LayoutControls()
        {
            if (!uiBuilt || Controls.Count < 2) return;

            int margin = 19;
            int w = ClientSize.Width;
            int y = 11;

            Controls[0].SetBounds(margin, y, 288, 27);
            Controls[1].SetBounds(margin, y + 29, w - margin * 2, 20);
            infoButton.SetBounds(w - margin - 26, y + 1, 26, 26);

            y += 61;
            int controlH = 26;
            AddField("Output", outputFormatBox, margin, y, 58, 170, controlH);
            AddField("Width", widthBox, margin + 299, y, 48, 62, controlH);
            AddField("Height", heightBox, margin + 419, y, 50, 62, controlH);
            AddField("Scale %", scaleBox, margin + 541, y, 54, 52, controlH);

            int listTop = y + 40;
            int bottomArea = 76;
            fileList.SetBounds(margin, listTop, w - margin * 2, Math.Max(144, ClientSize.Height - listTop - bottomArea));
            Invalidate();

            const int buttonW = 69;
            const int buttonH = 30;
            const int gap = 6;
            int totalButtonWidth = buttonW * 5 + gap * 4;
            int buttonX = Math.Max(margin, (w - totalButtonWidth) / 2);
            int buttonY = fileList.Bottom + 11;
            refreshButton.SetBounds(buttonX, buttonY, buttonW, buttonH);
            openInputButton.SetBounds(buttonX + (buttonW + gap), buttonY, buttonW, buttonH);
            openOutputButton.SetBounds(buttonX + (buttonW + gap) * 2, buttonY, buttonW, buttonH);
            convertButton.SetBounds(buttonX + (buttonW + gap) * 3, buttonY, buttonW, buttonH);
            clearButton.SetBounds(buttonX + (buttonW + gap) * 4, buttonY, buttonW, buttonH);
            cancelButton.SetBounds(w - margin - buttonW, buttonY, buttonW, buttonH);

            statusLabel.SetBounds(margin, buttonY + 35, w - margin * 2, 20);
            FitColumnsToList();
        }

        private void FitColumnsToList()
        {
            if (fittingColumns || fileList.Columns.Count != 4) return;
            fittingColumns = true;
            try
            {
                int clientWidth = Math.Max(0, fileList.ClientSize.Width);
                int fileWidth = Math.Max(120, Math.Min(160, clientWidth / 5));
                int typeWidth = 68;
                int sizeWidth = 82;
                int statusWidth = Math.Max(120, clientWidth - fileWidth - typeWidth - sizeWidth + 2);
                fileList.Columns[0].Width = fileWidth;
                fileList.Columns[1].Width = typeWidth;
                fileList.Columns[2].Width = sizeWidth;
                fileList.Columns[3].Width = statusWidth;
            }
            finally
            {
                fittingColumns = false;
            }
            HideHorizontalScrollBar();
        }

        private void AddField(string text, Control control, int x, int y, int labelWidth, int controlWidth, int controlHeight)
        {
            Label label = Controls.OfType<Label>().FirstOrDefault(l => (string)l.Tag == "field:" + text);
            if (label == null)
            {
                label = MakeLabel(text, normalFont, ColorMuted);
                label.Tag = "field:" + text;
                Controls.Add(label);
            }
            label.SetBounds(x, y + 6, labelWidth, 21);
            control.SetBounds(x + labelWidth + 3, y, controlWidth, controlHeight);
        }

        private Label MakeLabel(string text, Font font, Color color)
        {
            return new Label
            {
                Text = text,
                Font = font,
                ForeColor = color,
                BackColor = ColorBg,
                AutoSize = false
            };
        }

        private void ConfigureCombo(ComboBox combo, IEnumerable<object> values, int selected)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.FlatStyle = FlatStyle.Flat;
            combo.BackColor = ColorTab;
            combo.ForeColor = ColorText;
            combo.Font = normalFont;
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.ItemHeight = 20;
            combo.MaxDropDownItems = 14;
            combo.IntegralHeight = false;
            combo.DropDownHeight = combo.ItemHeight * 14 + 2;
            combo.DrawItem += DrawComboItem;
            combo.DropDown += delegate
            {
                combo.BeginInvoke(new Action(delegate
                {
                    combo.DroppedDown = false;
                    ShowFormatPopup();
                }));
            };
            combo.Items.AddRange(values.Cast<object>().ToArray());
            combo.SelectedIndex = selected;
        }

        private void DrawComboItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            ComboBox combo = (ComboBox)sender;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color bg = selected ? ColorAccent : ColorPanel;
            Color fg = selected ? Color.White : ColorText;
            using (SolidBrush brush = new SolidBrush(bg))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            Rectangle textBounds = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, combo.Items[e.Index].ToString(), normalFont, textBounds, fg, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void ShowFormatPopup()
        {
            if (formatPopup != null && !formatPopup.IsDisposed)
            {
                formatPopup.Close();
            }

            System.Drawing.Point location = outputFormatBox.PointToScreen(new System.Drawing.Point(0, outputFormatBox.Height));
            int width = outputFormatBox.Width;
            formatPopup = new FormatPopup(outputFormatBox, outputFormats, normalFont, width);
            formatPopup.ItemChosen += delegate(int index)
            {
                if (index >= 0 && index < outputFormatBox.Items.Count)
                {
                    outputFormatBox.SelectedIndex = index;
                }
            };
            formatPopup.Show(this);
            formatPopup.Location = location;
        }

        private void ShowSupportedFormatsDialog()
        {
            using (Form dialog = new Form())
            {
                dialog.Text = "Supported File Types";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ClientSize = new System.Drawing.Size(520, 430);
                dialog.BackColor = ColorBg;
                dialog.ForeColor = ColorText;
                dialog.Font = normalFont;
                dialog.Icon = Icon;

                Label title = MakeDialogLabel("Supported File Types", new Font("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Pixel), ColorText);
                title.SetBounds(18, 14, 300, 28);
                dialog.Controls.Add(title);

                TextBox list = new TextBox();
                list.Multiline = true;
                list.ReadOnly = true;
                list.TabStop = false;
                list.BorderStyle = BorderStyle.None;
                list.ScrollBars = ScrollBars.Vertical;
                list.BackColor = ColorPanel;
                list.ForeColor = ColorText;
                list.Font = normalFont;
                list.Text = SupportedFormatsText();
                list.SetBounds(18, 54, 484, 316);
                dialog.Controls.Add(list);

                StyledButton ok = new StyledButton("OK", ColorBlue, buttonFont);
                ok.SetBounds(dialog.ClientSize.Width - 18 - 69, dialog.ClientSize.Height - 45, 69, 30);
                ok.Click += delegate { dialog.Close(); };
                dialog.Controls.Add(ok);

                dialog.Shown += delegate
                {
                    int dark = 1;
                    DwmSetWindowAttribute(dialog.Handle, 20, ref dark, sizeof(int));
                    DwmSetWindowAttribute(dialog.Handle, 19, ref dark, sizeof(int));
                    list.SelectionStart = 0;
                    list.SelectionLength = 0;
                    ok.Focus();
                };
                dialog.ShowDialog(this);
            }
        }

        private Label MakeDialogLabel(string text, Font font, Color color)
        {
            return new Label
            {
                Text = text,
                Font = font,
                ForeColor = color,
                BackColor = ColorBg,
                AutoSize = false
            };
        }

        private string SupportedFormatsText()
        {
            return
                "Image input / output:\r\n" +
                "PNG (.png)\r\n" +
                "JPEG (.jpg, .jpeg)\r\n" +
                "WebP (.webp)\r\n" +
                "GIF (.gif)\r\n" +
                "BMP (.bmp)\r\n" +
                "TIFF (.tif, .tiff)\r\n" +
                "ICO (.ico)\r\n" +
                "SVG (.svg)\r\n" +
                "AVIF (.avif)\r\n" +
                "JPEG XL (.jxl)\r\n" +
                "JPEG 2000 (.jp2, .j2k, .j2c, .jpc)\r\n" +
                "SVGZ (.svgz)\r\n" +
                "CUR (.cur)\r\n" +
                "DDS (.dds)\r\n" +
                "EXR (.exr)\r\n" +
                "HDR (.hdr)\r\n" +
                "TGA (.tga, .icb, .vda, .vst)\r\n" +
                "PCX (.pcx)\r\n" +
                "PSD (.psd)\r\n" +
                "QOI (.qoi)\r\n" +
                "PNM / PBM / PGM / PPM / PAM\r\n" +
                "WBMP (.wbmp)\r\n" +
                "XBM (.xbm)\r\n" +
                "XPM (.xpm)\r\n" +
                "FITS (.fits)\r\n" +
                "DPX (.dpx)\r\n" +
                "JNG (.jng)\r\n" +
                "MNG (.mng)\r\n\r\n" +
                "Input only:\r\n" +
                "HEIF / HEIC (.heif, .heic)\r\n" +
                "PDF (.pdf)\r\n\r\n" +
                "Video input:\r\n" +
                "MP4 (.mp4), MOV (.mov), AVI (.avi), MKV (.mkv), WebM (.webm)";
        }

        private void ConfigureNumberText(TextBox box, string value)
        {
            box.BackColor = ColorTab;
            box.ForeColor = ColorText;
            box.BorderStyle = BorderStyle.None;
            box.Font = normalFont;
            box.Text = value;
            box.MaxLength = 6;
            box.TextAlign = HorizontalAlignment.Center;
            box.KeyPress += NumberTextBoxKeyPress;
            box.TextChanged += NumberTextBoxTextChanged;
        }

        private void DrawPurpleOutline(Graphics graphics, Rectangle bounds, int radius)
        {
            Rectangle rect = new Rectangle(bounds.X - 2, bounds.Y - 2, bounds.Width + 3, bounds.Height + 3);
            using (GraphicsPath path = RoundedRect(rect, radius))
            using (Pen pen = new Pen(ColorAccent, 1f))
            {
                graphics.DrawPath(pen, path);
            }
        }

        private void ConfigureScaleBox()
        {
            cancelButton.Visible = false;
            scaleBox.TextChanged += delegate
            {
                string digits = new string(scaleBox.Text.Where(char.IsDigit).ToArray());
                if (digits.Length > 0)
                {
                    int value;
                    if (int.TryParse(digits, out value) && value > 100)
                    {
                        digits = "100";
                    }
                }
                if (scaleBox.Text != digits)
                {
                    int selection = Math.Min(digits.Length, scaleBox.SelectionStart);
                    scaleBox.Text = digits;
                    scaleBox.SelectionStart = selection;
                }
            };
            scaleBox.Leave += delegate
            {
                int value;
                if (!int.TryParse(scaleBox.Text, out value))
                {
                    scaleBox.Text = "100";
                }
                else if (value < 5)
                {
                    scaleBox.Text = "5";
                }
                else if (value > 100)
                {
                    scaleBox.Text = "100";
                }
            };
        }

        private void NumberTextBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void NumberTextBoxTextChanged(object sender, EventArgs e)
        {
            TextBox box = (TextBox)sender;
            string digits = new string(box.Text.Where(char.IsDigit).ToArray());
            if (box.Text != digits)
            {
                int selection = Math.Min(digits.Length, box.SelectionStart);
                box.Text = digits;
                box.SelectionStart = selection;
            }
        }

        private void DrawListHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (SolidBrush bg = new SolidBrush(ColorTab))
            using (SolidBrush fg = new SolidBrush(ColorMuted))
            using (Pen border = new Pen(ColorAccent))
            {
                Rectangle bounds = e.Bounds;
                if (e.ColumnIndex == fileList.Columns.Count - 1)
                {
                    bounds.Width = Math.Max(bounds.Width, fileList.ClientSize.Width - bounds.X);
                }
                e.Graphics.FillRectangle(bg, bounds);
                e.Graphics.DrawRectangle(border, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                Rectangle textBounds = new Rectangle(bounds.X + 6, bounds.Y, bounds.Width - 8, bounds.Height);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, normalFont, textBounds, ColorMuted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        private void DrawListSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            Color bg = e.Item.Selected ? Blend(ColorPanel, ColorAccent, 0.22f) : ColorPanel;
            using (SolidBrush brush = new SolidBrush(bg))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            Color fg = ColorText;
            if (e.ColumnIndex == 3)
            {
                string value = e.SubItem.Text;
                if (value.IndexOf("Ready", StringComparison.OrdinalIgnoreCase) >= 0) fg = ColorGreen;
                else if (value.IndexOf("Video", StringComparison.OrdinalIgnoreCase) >= 0) fg = ColorYellow;
                else if (value.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0) fg = ColorRed;
                else if (value.IndexOf("Done", StringComparison.OrdinalIgnoreCase) >= 0) fg = ColorGreen;
            }
            else if (e.ColumnIndex == 1)
            {
                fg = e.SubItem.Text == "Video" ? ColorYellow : ColorBlue;
            }

            Rectangle textBounds = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, normalFont, textBounds, fg, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            using (Pen divider = new Pen(ColorAccent))
            {
                e.Graphics.DrawLine(divider, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }
        }

        private void RefreshFileList()
        {
            Directory.CreateDirectory(inputDir);
            Directory.CreateDirectory(outputDir);
            fileList.Items.Clear();

            foreach (string file in Directory.EnumerateFiles(inputDir).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                string ext = Path.GetExtension(file);
                bool isImage = imageExtensions.Contains(ext);
                bool isVideo = videoExtensions.Contains(ext);
                if (!isImage && !isVideo) continue;

                FileInfo info = new FileInfo(file);
                ListViewItem item = new ListViewItem(Path.GetFileName(file));
                item.Tag = file;
                item.SubItems.Add(isVideo ? "Video" : (string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase) ? "PDF" : "Image"));
                item.SubItems.Add(FormatBytes(info.Length));
                item.SubItems.Add(isVideo ? "Video ready" : "Ready");
                fileList.Items.Add(item);
            }

            SetStatus(fileList.Items.Count + " file(s) in Input.", ColorMuted);
            HideHorizontalScrollBar();
        }

        private async Task ConvertAllAsync()
        {
            if (fileList.Items.Count == 0)
            {
                SetStatus("Put files in Input or drag them onto the window.", ColorYellow);
                return;
            }

            ToggleBusy(true);
            conversionCancel = new CancellationTokenSource();
            CancellationToken token = conversionCancel.Token;
            int done = 0;
            int failed = 0;
            bool canceled = false;

            try
            {
                foreach (ListViewItem item in fileList.Items)
                {
                    token.ThrowIfCancellationRequested();
                    string path = (string)item.Tag;
                    bool isVideo = item.SubItems[1].Text == "Video";

                    try
                    {
                        item.SubItems[3].Text = "Working";
                        fileList.Invalidate(item.Bounds);
                        if (isVideo)
                        {
                            await ConvertVideoAsync(path, token);
                        }
                        else if (string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            await ConvertPdfAsync(path, token);
                        }
                        else
                        {
                            ConvertImage(path, token);
                        }
                        item.SubItems[3].Text = "Done";
                        done++;
                    }
                    catch (OperationCanceledException)
                    {
                        item.SubItems[3].Text = "Canceled";
                        canceled = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        item.SubItems[3].Text = "Error: " + ex.Message;
                        failed++;
                    }
                    fileList.Invalidate(item.Bounds);
                    SetStatus("Converted " + done + " file(s), " + failed + " failed.", failed > 0 ? ColorYellow : ColorGreen);
                    await Task.Delay(15);
                }
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            finally
            {
                ToggleBusy(false);
                conversionCancel.Dispose();
                conversionCancel = null;
            }

            SetStatus((canceled ? "Canceled. " : "Finished. ") + "Converted " + done + " file(s), " + failed + " failed.", canceled || failed > 0 ? ColorYellow : ColorGreen);
        }

        public int RunBatchForCommandLine()
        {
            RefreshFileList();
            int failed = 0;

            foreach (ListViewItem item in fileList.Items)
            {
                string path = (string)item.Tag;
                bool isVideo = item.SubItems[1].Text == "Video";
                try
                {
                    if (isVideo)
                    {
                        WaitWithMessagePump(ConvertVideoAsync(path, CancellationToken.None));
                    }
                    else if (string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        WaitWithMessagePump(ConvertPdfAsync(path, CancellationToken.None));
                    }
                    else
                    {
                        ConvertImage(path, CancellationToken.None);
                    }
                }
                catch
                {
                    failed++;
                }
            }

            return failed == 0 ? 0 : 1;
        }

        private static void WaitWithMessagePump(Task task)
        {
            while (!task.IsCompleted)
            {
                Application.DoEvents();
                Thread.Sleep(15);
            }
            task.GetAwaiter().GetResult();
        }

        private void ConvertImage(string sourcePath, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            OutputFormat format = SelectedFormat();
            string targetPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(sourcePath) + "-conv" + format.PrimaryExtension);

            using (MagickImageCollection images = new MagickImageCollection(sourcePath))
            {
                token.ThrowIfCancellationRequested();
                if (images.Count == 0)
                {
                    throw new InvalidOperationException("No image frames found");
                }

                string resize = BuildMagickResize();
                if (!string.IsNullOrEmpty(resize))
                {
                    MagickGeometry geometry = new MagickGeometry(resize);
                    foreach (MagickImage image in images)
                    {
                        token.ThrowIfCancellationRequested();
                        image.Resize(geometry);
                    }
                }

                foreach (MagickImage image in images)
                {
                    token.ThrowIfCancellationRequested();
                    if (format.MaxDimension > 0 && (image.Width > format.MaxDimension || image.Height > format.MaxDimension))
                    {
                        throw new InvalidOperationException(format.Name + " supports max " + format.MaxDimension + "x" + format.MaxDimension + ". Lower Width, Height, or Scale %.");
                    }
                    image.Format = format.MagickFormat;
                    if (format.MagickFormat == MagickFormat.Jpeg)
                    {
                        image.BackgroundColor = MagickColors.White;
                        image.Alpha(AlphaOption.Remove);
                    }
                }

                if (images.Count > 1 && format.AnimationCapable)
                {
                    token.ThrowIfCancellationRequested();
                    images.Write(targetPath);
                }
                else
                {
                    token.ThrowIfCancellationRequested();
                    images[0].Write(targetPath);
                }
            }
        }

        private OutputFormat SelectedFormat()
        {
            OutputFormat format = outputFormatBox.SelectedItem as OutputFormat;
            return format ?? outputFormats[0];
        }

        private int LoadSelectedFormatIndex()
        {
            try
            {
                string path = SettingsPath();
                if (!File.Exists(path)) return 0;
                string ext = File.ReadAllText(path).Trim();
                for (int i = 0; i < outputFormats.Count; i++)
                {
                    if (string.Equals(outputFormats[i].PrimaryExtension, ext, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }
            catch
            {
            }
            return 0;
        }

        private void SaveSelectedFormat()
        {
            try
            {
                OutputFormat format = SelectedFormat();
                File.WriteAllText(SettingsPath(), format.PrimaryExtension);
            }
            catch
            {
            }
        }

        private string SettingsPath()
        {
            return Path.Combine(exeDir, "last_format.txt");
        }

        private void ShowSelectedFormatWarning()
        {
            OutputFormat format = SelectedFormat();
            if (format.MaxDimension > 0)
            {
                SetStatus(format.Name + " supports max " + format.MaxDimension + "x" + format.MaxDimension + ". Lower Width, Height, or Scale %.", ColorYellow);
            }
        }

        private string BuildMagickResize()
        {
            int requestedWidth = ParsePositiveInt(widthBox.Text);
            int requestedHeight = ParsePositiveInt(heightBox.Text);
            if (requestedWidth <= 0 && requestedHeight <= 0)
            {
                int scale = ParseScalePercent();
                return scale >= 100 ? string.Empty : scale + "%";
            }

            string width = requestedWidth > 0 ? requestedWidth.ToString() : string.Empty;
            string height = requestedHeight > 0 ? requestedHeight.ToString() : string.Empty;
            return width + "x" + height + ">";
        }

        private async Task ConvertPdfAsync(string sourcePath, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            OutputFormat format = SelectedFormat();
            if (format.AnimationCapable || format.MagickFormat == MagickFormat.Svg || format.MagickFormat == MagickFormat.Ico || format.MagickFormat == MagickFormat.Cur)
            {
                throw new InvalidOperationException("Choose a still raster format for PDF");
            }

            StorageFile file = await WinRtTask(StorageFile.GetFileFromPathAsync(sourcePath));
            PdfDocument document = await WinRtTask(PdfDocument.LoadFromFileAsync(file));
            if (document.PageCount == 0)
            {
                throw new InvalidOperationException("PDF has no pages");
            }

            string baseName = Path.GetFileNameWithoutExtension(sourcePath) + "-conv";
            string pageDir = document.PageCount > 1 ? Path.Combine(outputDir, baseName) : outputDir;
            Directory.CreateDirectory(pageDir);

            for (uint pageIndex = 0; pageIndex < document.PageCount; pageIndex++)
            {
                token.ThrowIfCancellationRequested();
                using (PdfPage page = document.GetPage(pageIndex))
                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    int targetWidth;
                    int targetHeight;
                    GetTargetSize((int)Math.Ceiling(page.Size.Width), (int)Math.Ceiling(page.Size.Height), out targetWidth, out targetHeight);
                    PdfPageRenderOptions options = new PdfPageRenderOptions
                    {
                        DestinationWidth = (uint)targetWidth,
                        DestinationHeight = (uint)targetHeight
                    };
                    await WinRtTask(page.RenderToStreamAsync(stream, options));
                    byte[] bytes = await ReadRandomAccessStream(stream);
                    using (MagickImage image = new MagickImage(bytes))
                    {
                        image.Format = format.MagickFormat;
                        if (format.MagickFormat == MagickFormat.Jpeg)
                        {
                            image.BackgroundColor = MagickColors.White;
                            image.Alpha(AlphaOption.Remove);
                        }
                        string path = document.PageCount > 1
                            ? Path.Combine(pageDir, baseName + "-" + (pageIndex + 1).ToString("000000") + format.PrimaryExtension)
                            : Path.Combine(pageDir, baseName + format.PrimaryExtension);
                        image.Write(path);
                    }
                }
            }
        }

        private async Task ConvertVideoAsync(string sourcePath, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            OutputFormat format = SelectedFormat();
            if (format.MagickFormat == MagickFormat.Svg || format.MagickFormat == MagickFormat.Ico)
            {
                throw new InvalidOperationException("Choose GIF, WebP, or a normal still-image format for video");
            }

            string baseName = Path.GetFileNameWithoutExtension(sourcePath) + "-conv";
            bool outputFrames = !format.AnimationCapable;
            const int fps = 10;

            using (VideoFrameReader reader = new VideoFrameReader(sourcePath))
            {
                await reader.OpenAsync();
                token.ThrowIfCancellationRequested();
                int targetWidth;
                int targetHeight;
                GetTargetSize(reader.Width, reader.Height, out targetWidth, out targetHeight);
                int frameCount = Math.Max(1, (int)Math.Ceiling(reader.Duration.TotalSeconds * fps));

                if (outputFrames)
                {
                    string sequenceDir = Path.Combine(outputDir, baseName);
                    Directory.CreateDirectory(sequenceDir);
                    for (int i = 0; i < frameCount; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        TimeSpan position = TimeSpan.FromSeconds((double)i / fps);
                        using (MagickImage frame = await reader.CaptureAsync(position, targetWidth, targetHeight))
                        {
                            frame.Format = format.MagickFormat;
                            if (format.MagickFormat == MagickFormat.Jpeg)
                            {
                                frame.BackgroundColor = MagickColors.White;
                                frame.Alpha(AlphaOption.Remove);
                            }
                            string framePath = Path.Combine(sequenceDir, baseName + "-" + (i + 1).ToString("000000") + format.PrimaryExtension);
                            frame.Write(framePath);
                        }
                    }
                    return;
                }

                string targetPath = Path.Combine(outputDir, baseName + format.PrimaryExtension);
                using (MagickImageCollection frames = new MagickImageCollection())
                {
                    for (int i = 0; i < frameCount; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        TimeSpan position = TimeSpan.FromSeconds((double)i / fps);
                        MagickImage frame = await reader.CaptureAsync(position, targetWidth, targetHeight);
                        frame.Format = format.MagickFormat;
                        frame.AnimationDelay = 100 / fps;
                        frames.Add(frame);
                    }
                    token.ThrowIfCancellationRequested();
                    frames.Write(targetPath);
                }
            }
        }

        private void GetTargetSize(int inputWidth, int inputHeight, out int targetWidth, out int targetHeight)
        {
            int requestedWidth = ParsePositiveInt(widthBox.Text);
            int requestedHeight = ParsePositiveInt(heightBox.Text);
            targetWidth = inputWidth;
            targetHeight = inputHeight;

            if (requestedWidth <= 0 && requestedHeight <= 0)
            {
                int scale = ParseScalePercent();
                if (scale >= 100) return;
                targetWidth = Math.Max(1, (int)Math.Round(inputWidth * (scale / 100.0)));
                targetHeight = Math.Max(1, (int)Math.Round(inputHeight * (scale / 100.0)));
                return;
            }

            requestedWidth = requestedWidth <= 0 ? inputWidth : Math.Min(requestedWidth, inputWidth);
            requestedHeight = requestedHeight <= 0 ? inputHeight : Math.Min(requestedHeight, inputHeight);

            double ratio = Math.Min((double)requestedWidth / inputWidth, (double)requestedHeight / inputHeight);
            ratio = Math.Min(1.0, ratio);
            targetWidth = Math.Max(1, (int)Math.Round(inputWidth * ratio));
            targetHeight = Math.Max(1, (int)Math.Round(inputHeight * ratio));
        }

        private int ParsePositiveInt(string text)
        {
            int value;
            if (!int.TryParse((text ?? string.Empty).Trim(), out value)) return 0;
            return Math.Max(0, value);
        }

        private int ParseScalePercent()
        {
            int value;
            if (!int.TryParse((scaleBox.Text ?? string.Empty).Trim(), out value)) return 100;
            return Math.Max(5, Math.Min(100, value));
        }

        private static Task<T> WinRtTask<T>(IAsyncOperation<T> operation)
        {
            TaskCompletionSource<T> source = new TaskCompletionSource<T>();
            operation.Completed = delegate(IAsyncOperation<T> asyncInfo, AsyncStatus status)
            {
                if (status == AsyncStatus.Completed)
                {
                    source.TrySetResult(asyncInfo.GetResults());
                }
                else if (status == AsyncStatus.Canceled)
                {
                    source.TrySetCanceled();
                }
                else
                {
                    source.TrySetException(asyncInfo.ErrorCode ?? new InvalidOperationException("Windows operation failed"));
                }
            };
            return source.Task;
        }

        private static Task WinRtTask(IAsyncAction operation)
        {
            TaskCompletionSource<bool> source = new TaskCompletionSource<bool>();
            operation.Completed = delegate(IAsyncAction asyncInfo, AsyncStatus status)
            {
                if (status == AsyncStatus.Completed)
                {
                    source.TrySetResult(true);
                }
                else if (status == AsyncStatus.Canceled)
                {
                    source.TrySetCanceled();
                }
                else
                {
                    source.TrySetException(asyncInfo.ErrorCode ?? new InvalidOperationException("Windows operation failed"));
                }
            };
            return source.Task;
        }

        private static async Task<byte[]> ReadRandomAccessStream(IRandomAccessStream stream)
        {
            stream.Seek(0);
            DataReader reader = new DataReader(stream.GetInputStreamAt(0));
            await WinRtTask(reader.LoadAsync((uint)stream.Size));
            if (stream.Size > int.MaxValue)
            {
                throw new InvalidOperationException("Rendered PDF page is too large");
            }
            byte[] bytes = new byte[(int)stream.Size];
            reader.ReadBytes(bytes);
            reader.DetachStream();
            return bytes;
        }

        private IEnumerable<string> ExpandDroppedFiles(IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    yield return path;
                }
                else if (Directory.Exists(path))
                {
                    foreach (string file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                    {
                        yield return file;
                    }
                }
            }
        }

        private bool IsDuplicateInput(string sourcePath)
        {
            string targetPath = Path.Combine(inputDir, Path.GetFileName(sourcePath));
            return File.Exists(targetPath);
        }

        private void ClearInput()
        {
            try
            {
                foreach (string file in Directory.EnumerateFiles(inputDir))
                {
                    File.Delete(file);
                }
                foreach (string dir in Directory.EnumerateDirectories(inputDir))
                {
                    Directory.Delete(dir, true);
                }
                RefreshFileList();
                SetStatus("Input cleared.", ColorRed);
            }
            catch (Exception ex)
            {
                SetStatus("Could not clear Input: " + ex.Message, ColorRed);
            }
        }

        private void OpenFolder(string folder)
        {
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
        }

        private void ToggleBusy(bool busy)
        {
            convertButton.Enabled = !busy;
            refreshButton.Enabled = !busy;
            clearButton.Enabled = !busy;
            openInputButton.Enabled = !busy;
            openOutputButton.Enabled = !busy;
            cancelButton.Visible = busy;
            cancelButton.Enabled = busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private void SetStatus(string text, Color color)
        {
            statusLabel.Text = text;
            statusLabel.ForeColor = color;
        }

        private void HideHorizontalScrollBar()
        {
            if (fileList.IsHandleCreated)
            {
                ShowScrollBar(fileList.Handle, 0, false);
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            double value = bytes;
            int index = 0;
            while (value >= 1024 && index < suffixes.Length - 1)
            {
                value /= 1024;
                index++;
            }
            return value.ToString(index == 0 ? "0" : "0.0") + " " + suffixes[index];
        }

        private static Color Blend(Color a, Color b, float amount)
        {
            amount = Math.Max(0, Math.Min(1, amount));
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * amount),
                (int)(a.G + (b.G - a.G) * amount),
                (int)(a.B + (b.B - a.B) * amount));
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void SetDarkTitleBar()
        {
            if (!IsHandleCreated) return;
            int dark = 1;
            DwmSetWindowAttribute(Handle, 20, ref dark, sizeof(int));
            DwmSetWindowAttribute(Handle, 19, ref dark, sizeof(int));
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

        private sealed class FormatPopup : Form
        {
            private const int FullVisibleRows = 13;
            private const int HalfRowPixels = 10;
            private readonly ComboBox owner;
            private readonly List<OutputFormat> formats;
            private readonly Font font;
            private readonly int rowHeight;
            private int scrollOffset;
            private int hoverIndex = -1;

            public event Action<int> ItemChosen;

            public FormatPopup(ComboBox owner, List<OutputFormat> formats, Font font, int width)
            {
                this.owner = owner;
                this.formats = formats;
                this.font = font;
                rowHeight = 20;

                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                BackColor = ColorPanel;
                int visibleHeight = rowHeight * Math.Min(FullVisibleRows, formats.Count);
                if (formats.Count > FullVisibleRows)
                {
                    visibleHeight += HalfRowPixels;
                }
                Size = new System.Drawing.Size(width, visibleHeight + 2);
                DoubleBuffered = true;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                UpdateStyles();
            }

            protected override void OnShown(EventArgs e)
            {
                base.OnShown(e);
                scrollOffset = 0;
                hoverIndex = owner.SelectedIndex >= 0 && owner.SelectedIndex < FullVisibleRows ? owner.SelectedIndex : -1;
                Invalidate();
                Activate();
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
            }

            protected override void OnDeactivate(EventArgs e)
            {
                Close();
                base.OnDeactivate(e);
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                int direction = e.Delta < 0 ? 1 : -1;
                int maxOffset = Math.Max(0, TotalContentHeight() - (Height - 2));
                scrollOffset = Math.Max(0, Math.Min(maxOffset, scrollOffset + direction * rowHeight));
                UpdateHover(e.Location);
                Invalidate();
                base.OnMouseWheel(e);
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                UpdateHover(e.Location);
                Invalidate();
                base.OnMouseMove(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                hoverIndex = -1;
                Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                int index = IndexAtOffset(e.Y - 1 + scrollOffset);
                if (index >= 0 && index < formats.Count)
                {
                    if (ItemChosen != null) ItemChosen(index);
                    Close();
                }
                base.OnMouseDown(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush bg = new SolidBrush(ColorPanel))
                using (Pen border = new Pen(ColorAccent))
                {
                    e.Graphics.FillRectangle(bg, ClientRectangle);
                    e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
                }

                int firstIndex = IndexAtOffset(scrollOffset);
                int top = 1 - (scrollOffset - OffsetForIndex(firstIndex));
                for (int index = firstIndex; index < formats.Count; index++)
                {
                    OutputFormat format = formats[index];
                    Rectangle bounds = new Rectangle(1, top, Width - 2, rowHeight);
                    if (bounds.Bottom <= 1 || bounds.Top >= Height - 1) continue;
                    bool selected = index == owner.SelectedIndex;
                    bool hover = index == hoverIndex;
                    Color bg = selected ? ColorAccent : (hover ? Blend(ColorPanel, ColorAccent, 0.22f) : ColorPanel);
                    Color fg = selected ? Color.White : ColorText;
                    using (SolidBrush fill = new SolidBrush(bg))
                    {
                        e.Graphics.FillRectangle(fill, bounds);
                    }
                    Rectangle textBounds = new Rectangle(bounds.X + 6, bounds.Y, bounds.Width - 10, bounds.Height);
                    TextRenderer.DrawText(e.Graphics, format.ToString(), font, textBounds, fg, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    top += rowHeight;
                    if (top >= Height - 1) break;
                }
            }

            private void UpdateHover(System.Drawing.Point point)
            {
                int index = IndexAtOffset(point.Y - 1 + scrollOffset);
                hoverIndex = point.Y > 0 && point.Y < Height - 1 && index >= 0 && index < formats.Count ? index : -1;
            }

            private int TotalContentHeight()
            {
                return formats.Count * rowHeight;
            }

            private int OffsetForIndex(int index)
            {
                return Math.Max(0, Math.Min(index, formats.Count)) * rowHeight;
            }

            private int IndexAtOffset(double offset)
            {
                if (formats.Count == 0) return -1;
                int index = (int)Math.Floor(offset / rowHeight);
                return Math.Max(0, Math.Min(formats.Count - 1, index));
            }
        }
    }

    internal sealed class VideoFrameReader : IDisposable
    {
        private readonly System.Windows.Media.MediaPlayer player = new System.Windows.Media.MediaPlayer();
        private readonly string path;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public TimeSpan Duration { get; private set; }

        public VideoFrameReader(string path)
        {
            this.path = path;
            player.ScrubbingEnabled = true;
            player.Volume = 0;
        }

        public async Task OpenAsync()
        {
            TaskCompletionSource<bool> opened = new TaskCompletionSource<bool>();
            EventHandler openedHandler = null;
            EventHandler<System.Windows.Media.ExceptionEventArgs> failedHandler = null;

            openedHandler = delegate
            {
                opened.TrySetResult(true);
            };
            failedHandler = delegate(object sender, System.Windows.Media.ExceptionEventArgs e)
            {
                opened.TrySetException(e.ErrorException ?? new InvalidOperationException("Windows could not open this video"));
            };

            player.MediaOpened += openedHandler;
            player.MediaFailed += failedHandler;
            player.Open(new Uri(path, UriKind.Absolute));

            try
            {
                await WaitWithMessagePumpAsync(opened.Task, 15000);
            }
            finally
            {
                player.MediaOpened -= openedHandler;
                player.MediaFailed -= failedHandler;
            }

            if (!player.NaturalDuration.HasTimeSpan || player.NaturalVideoWidth <= 0 || player.NaturalVideoHeight <= 0)
            {
                throw new InvalidOperationException("Windows cannot decode this video");
            }

            Duration = player.NaturalDuration.TimeSpan;
            Width = player.NaturalVideoWidth;
            Height = player.NaturalVideoHeight;
        }

        public async Task<MagickImage> CaptureAsync(TimeSpan position, int width, int height)
        {
            if (position > Duration) position = Duration;
            player.Position = position;
            await PumpDelayAsync(120);

            System.Windows.Media.DrawingVisual visual = new System.Windows.Media.DrawingVisual();
            using (System.Windows.Media.DrawingContext context = visual.RenderOpen())
            {
                context.DrawVideo(player, new System.Windows.Rect(0, 0, width, height));
            }

            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(visual);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            MemoryStream stream = new MemoryStream();
            encoder.Save(stream);
            stream.Position = 0;
            MagickImage image = new MagickImage(stream);
            stream.Dispose();
            return image;
        }

        private static async Task WaitWithMessagePumpAsync(Task task, int timeoutMs)
        {
            Stopwatch watch = Stopwatch.StartNew();
            while (!task.IsCompleted && watch.ElapsedMilliseconds < timeoutMs)
            {
                Application.DoEvents();
                await Task.Delay(25);
            }

            if (!task.IsCompleted)
            {
                throw new InvalidOperationException("Timed out opening video");
            }

            await task;
        }

        private static async Task PumpDelayAsync(int delayMs)
        {
            Stopwatch watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < delayMs)
            {
                Application.DoEvents();
                await Task.Delay(15);
            }
        }

        public void Dispose()
        {
            player.Close();
        }
    }

    internal sealed class OutputFormat
    {
        public readonly string Name;
        public readonly string PrimaryExtension;
        public readonly MagickFormat MagickFormat;
        public readonly bool AnimationCapable;
        public readonly int MaxDimension;
        private readonly string[] aliases;

        public OutputFormat(string name, string primaryExtension, MagickFormat magickFormat, bool animationCapable)
            : this(name, primaryExtension, magickFormat, animationCapable, 0, new string[0])
        {
        }

        public OutputFormat(string name, string primaryExtension, MagickFormat magickFormat, bool animationCapable, params string[] aliases)
            : this(name, primaryExtension, magickFormat, animationCapable, 0, aliases)
        {
        }

        public OutputFormat(string name, string primaryExtension, MagickFormat magickFormat, bool animationCapable, int maxDimension, params string[] aliases)
        {
            Name = name;
            PrimaryExtension = primaryExtension;
            MagickFormat = magickFormat;
            AnimationCapable = animationCapable;
            MaxDimension = maxDimension;
            this.aliases = aliases ?? new string[0];
        }

        public override string ToString()
        {
            string suffix = PrimaryExtension;
            if (aliases.Length > 0)
            {
                suffix += ", " + string.Join(", ", aliases);
            }
            return Name + " (" + suffix + ")";
        }
    }

    internal sealed class StyledButton : Button
    {
        private readonly Color accent;
        private bool pressed;
        private bool hovered;

        public StyledButton(string text, Color accent, Font font)
        {
            this.accent = accent;
            Text = text;
            Font = font;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.FromArgb(25, 25, 25);
            ForeColor = accent;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            pressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            pevent.Graphics.Clear(Color.FromArgb(25, 25, 25));
            Color bg = Color.FromArgb(25, 25, 25);
            Color border = Enabled ? accent : Color.FromArgb(72, 72, 72);
            Color fg = Enabled ? accent : Color.FromArgb(100, 100, 100);
            if (hovered && Enabled)
            {
                bg = Blend(bg, accent, 0.08f);
            }
            if (pressed && Enabled)
            {
                bg = Blend(bg, accent, 0.18f);
                border = Blend(accent, Color.White, 0.25f);
                fg = border;
            }

            Rectangle rect = new Rectangle(1, 1, Width - 3, Height - 3);
            using (GraphicsPath path = RoundedRect(rect, 7))
            using (SolidBrush fill = new SolidBrush(bg))
            using (Pen pen = new Pen(border, 1f))
            {
                pevent.Graphics.FillPath(fill, path);
                pevent.Graphics.DrawPath(pen, path);
            }

            TextRenderer.DrawText(pevent.Graphics, Text, Font, rect, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Color Blend(Color a, Color b, float amount)
        {
            amount = Math.Max(0, Math.Min(1, amount));
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * amount),
                (int)(a.G + (b.G - a.G) * amount),
                (int)(a.B + (b.B - a.B) * amount));
        }
    }
}
