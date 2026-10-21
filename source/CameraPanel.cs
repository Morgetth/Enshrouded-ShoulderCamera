using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShoulderCamera {
    [DataContract] internal sealed class ProfilePreferences {
        [DataMember] public int horizontal;
        [DataMember] public int height;
        [DataMember] public bool enabled;
        internal ProfilePreferences(int x,int y,bool on) {horizontal=x;height=y;enabled=on;}
        internal void Clamp() {horizontal=Math.Max(-150,Math.Min(150,horizontal));height=Math.Max(-75,Math.Min(75,height));}
        internal CameraProfile ToProfile() {return new CameraProfile(enabled,horizontal/100f,height/100f);}
    }
    [DataContract] internal sealed class Preferences {
        [DataMember] public int version=2;
        [DataMember] public ProfilePreferences exploration,bow,staff;
        [DataMember(EmitDefaultValue=false)] public int? horizontal;
        [DataMember(EmitDefaultValue=false)] public int? height;
        [DataMember(EmitDefaultValue=false)] public bool? applyToAiming;
        internal void Normalize() {
            bool legacyAim=applyToAiming??false;int x=horizontal??45,y=height??0;
            exploration=exploration??new ProfilePreferences(x,y,true);
            bow=bow??new ProfilePreferences(legacyAim?x:45,legacyAim?y:0,legacyAim);
            staff=staff??new ProfilePreferences(legacyAim?x:120,legacyAim?y:0,legacyAim);
            exploration.Clamp();bow.Clamp();staff.Clamp();version=2;horizontal=null;height=null;applyToAiming=null;
        }
        internal static Preferences Read(Stream stream) {
            var p=(Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(stream);
            if(p==null)throw new InvalidDataException();p.Normalize();return p;
        }
        internal void Write(Stream stream) {Normalize();new DataContractJsonSerializer(typeof(Preferences)).WriteObject(stream,this);}
        internal static Preferences Defaults() {var p=new Preferences();p.Normalize();return p;}
        internal static void SelfTest() {
            Preferences p;
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes("{\"horizontal\":58,\"height\":-7,\"applyToAiming\":false}")))p=Read(stream);
            if(p.exploration.horizontal!=58 || p.exploration.height!=-7 || p.bow.enabled || p.staff.enabled || p.staff.horizontal!=120)throw new Exception("Migration v1 incorrecte");
            p.bow=new ProfilePreferences(-40,10,true);p.staff=new ProfilePreferences(80,-15,true);
            using(var stream=new MemoryStream()) {
                p.Write(stream);string json=Encoding.UTF8.GetString(stream.ToArray());
                if(json.Contains("applyToAiming") || json.Contains("active") || json.Contains("address"))throw new Exception("Preference privee ou activation persistante");
                stream.Position=0;var loaded=Read(stream);
                if(loaded.bow.horizontal!=-40 || loaded.staff.horizontal!=80 || loaded.exploration.horizontal!=58)throw new Exception("Profils melanges pendant la sauvegarde");
            }
            Console.WriteLine("PASS: v1 preference migration, independent profile round trip, no saved activation or address.");
        }
    }
    [DataContract] internal sealed class Configurations {
        [DataMember] public int version=3;
        [DataMember] public int selectedSlot;
        [DataMember] public string language="en";
        [DataMember] public Preferences[] slots;
        internal Preferences Current {get{return slots[selectedSlot];}}
        internal static Preferences Clone(Preferences p){using(var stream=new MemoryStream()){p.Write(stream);stream.Position=0;return Preferences.Read(stream);}}
        internal void Normalize(){
            var old=slots;slots=new Preferences[3];
            for(int i=0;i<3;i++){slots[i]=old!=null && i<old.Length && old[i]!=null?Clone(old[i]):Preferences.Defaults();slots[i].Normalize();}
            selectedSlot=Math.Max(0,Math.Min(2,selectedSlot));language=Localizer.Normalize(language);version=3;
        }
        internal static Configurations FromLegacy(Preferences p){return new Configurations {slots=new Preferences[]{Clone(p),Clone(p),Clone(p)}};}
        internal static Configurations Defaults(){return FromLegacy(Preferences.Defaults());}
        internal static Configurations Read(Stream input){
            using(var buffer=new MemoryStream()){
                input.CopyTo(buffer);buffer.Position=0;
                var result=(Configurations)new DataContractJsonSerializer(typeof(Configurations)).ReadObject(buffer);
                if(result==null)throw new InvalidDataException();
                if(result.slots==null){buffer.Position=0;return FromLegacy(Preferences.Read(buffer));}
                result.Normalize();return result;
            }
        }
        internal void Write(Stream output){new DataContractJsonSerializer(typeof(Configurations)).WriteObject(output,this);}
        internal static void SelfTest(){
            Configurations c;
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes("{\"version\":2,\"exploration\":{\"horizontal\":98,\"height\":-10,\"enabled\":true},\"bow\":{\"horizontal\":15,\"height\":67,\"enabled\":false}}")))c=Read(stream);
            if(c.selectedSlot!=0 || c.slots[0].exploration.horizontal!=98 || c.slots[0].bow.enabled)throw new Exception("Configuration migration failed");
            if(c.language!="en")throw new Exception("Default language failed");
            c.slots[1].exploration.horizontal=-33;c.slots[2].bow.enabled=true;c.selectedSlot=2;c.language="de";
            using(var stream=new MemoryStream()){c.Write(stream);stream.Position=0;c=Read(stream);}
            if(c.language!="de" || c.selectedSlot!=2 || c.slots[0].exploration.horizontal!=98 || c.slots[1].exploration.horizontal!=-33 || c.slots[0].bow.enabled || !c.slots[2].bow.enabled)throw new Exception("Configuration isolation failed");
            c.selectedSlot=99;c.Normalize();if(c.selectedSlot!=2 || c.slots.Length!=3)throw new Exception("Configuration bounds failed");
            Console.WriteLine("PASS: v2 migration, three independent configuration slots, selected slot round trip, slot bounds.");
        }
    }
    internal static class NumericInput {
        internal static int Parse(string text,int previous,int minimum,int maximum) {
            double value;
            if(!double.TryParse(text.Trim().Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out value) || double.IsNaN(value))return previous;
            value=Math.Max(minimum/100.0,Math.Min(maximum/100.0,value));
            return (int)Math.Round(value*100,MidpointRounding.AwayFromZero);
        }
        internal static string Format(int value){return (value/100.0).ToString("+0.00;-0.00;0.00",CultureInfo.InvariantCulture);}
        internal static void SelfTest(){
            if(Parse("99",0,-150,150)!=150 || Parse("-99",0,-150,150)!=-150 || Parse("0,58",0,-150,150)!=58 || Parse("NaN",42,-150,150)!=42 || Parse("abc",42,-150,150)!=42 || Parse("1.50",0,-75,75)!=75 || Parse("-1e20",0,-75,75)!=-75)throw new Exception("Numeric input bounds failed");
            Console.WriteLine("PASS: numeric input clamp, comma, invalid input, height limits.");
        }
    }
    internal sealed class BufferedLabel : Label {
        internal BufferedLabel(){SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
    }
    internal static class ShroudTheme {
        internal static Color Gray(Color color){int v=(int)Math.Round(color.R*0.299+color.G*0.587+color.B*0.114);return Color.FromArgb(color.A,v,v,v);}
        internal static readonly Color Background=Color.FromArgb(9,20,29),Panel=Color.FromArgb(17,36,48),PanelLight=Color.FromArgb(23,47,61);
        internal static readonly Color Gold=Color.FromArgb(205,176,119),Text=Color.FromArgb(234,237,226),Muted=Color.FromArgb(154,179,190),Blue=Color.FromArgb(115,185,207);
    }
    internal sealed class ShroudButton : Button {
        bool hover,selected;
        bool? profileActive;
        internal bool? ProfileActive {get{return profileActive;}set{profileActive=value;AccessibleDescription=value.HasValue?Localizer.Get(value.Value?"Profil activé":"Profil désactivé"):null;Invalidate();}}
        internal bool Selected {get{return selected;}set{selected=value;Invalidate();}}
        internal ShroudButton() {SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;BackColor=ShroudTheme.Panel;ForeColor=ShroudTheme.Text;Font=new Font("Segoe UI",10,FontStyle.Bold);}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnPaint(PaintEventArgs e) {
            Rectangle r=new Rectangle(0,0,Math.Max(1,Width-1),Math.Max(1,Height-1));
            e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            int cut=7;Point[] outline={new Point(cut,1),new Point(Width-cut-1,1),new Point(Width-2,cut),new Point(Width-2,Height-cut-1),new Point(Width-cut-1,Height-2),new Point(cut,Height-2),new Point(1,Height-cut-1),new Point(1,cut)};
            Color top=hover||selected?Color.FromArgb(46,78,91):ShroudTheme.PanelLight,bottom=ShroudTheme.Panel,edge=hover||selected?ShroudTheme.Gold:Color.FromArgb(118,132,127);
            if(!Enabled){top=ShroudTheme.Gray(top);bottom=ShroudTheme.Gray(bottom);edge=Color.FromArgb(91,91,91);}
            using(var b=new LinearGradientBrush(r,top,bottom,90))e.Graphics.FillPolygon(b,outline);
            using(var p=new Pen(edge,selected?2:1))e.Graphics.DrawPolygon(p,outline);
            using(var p=new Pen(Color.FromArgb(70,edge))){e.Graphics.DrawLine(p,cut+4,5,Width-cut-5,5);e.Graphics.DrawLine(p,cut+4,Height-6,Width-cut-5,Height-6);}
            Rectangle textBounds=r;
            if(profileActive.HasValue){
                int y=Height/2;Color indicator=profileActive.Value?ShroudTheme.Gold:Color.FromArgb(150,150,150);
                Point[] diamond={new Point(9,y),new Point(14,y-5),new Point(19,y),new Point(14,y+5)};
                if(profileActive.Value)using(var b=new SolidBrush(indicator))e.Graphics.FillPolygon(b,diamond);
                using(var p=new Pen(indicator,1.5f))e.Graphics.DrawPolygon(p,diamond);
                textBounds=new Rectangle(23,7,Width-34,Height-25);
                using(var small=new Font("Segoe UI",7.5f))TextRenderer.DrawText(e.Graphics,Localizer.Get(profileActive.Value?"ACTIVÉ":"DÉSACTIVÉ"),small,new Rectangle(23,Height-23,Width-34,15),indicator,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            }
            TextRenderer.DrawText(e.Graphics,Text,Font,textBounds,Enabled?(profileActive==false?Color.FromArgb(173,173,173):selected||hover?ShroudTheme.Gold:ShroudTheme.Text):Color.FromArgb(153,153,153),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
            if(Focused)using(var p=new Pen(edge)){p.DashStyle=DashStyle.Dot;e.Graphics.DrawRectangle(p,8,8,Width-17,Height-17);}
        }
    }
    internal sealed class ShroudCheckBox : CheckBox {
        bool hover;
        internal ShroudCheckBox(){FlatStyle=FlatStyle.Flat;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Opaque,true);AutoSize=false;Size=new Size(185,30);Cursor=Cursors.Hand;}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnCheckedChanged(EventArgs e){base.OnCheckedChanged(e);Invalidate();}
        protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(BackColor);}
        protected override void OnPaint(PaintEventArgs e){
            using(var background=new SolidBrush(BackColor))e.Graphics.FillRectangle(background,ClientRectangle);
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            int y=Height/2;
            Color accent=Enabled?(Checked?ShroudTheme.Gold:ForeColor):Color.FromArgb(97,97,97);
            Point[] frame={new Point(3,y-9),new Point(17,y-9),new Point(20,y-6),new Point(20,y+6),new Point(17,y+9),new Point(3,y+9),new Point(0,y+6),new Point(0,y-6)};
            using(var b=new SolidBrush(Enabled?Color.FromArgb(35,ForeColor):Color.FromArgb(27,27,27)))e.Graphics.FillPolygon(b,frame);
            using(var p=new Pen(hover&&Enabled?ShroudTheme.Gold:accent,1.3f))e.Graphics.DrawPolygon(p,frame);
            if(Checked)using(var b=new SolidBrush(accent))e.Graphics.FillPolygon(b,new Point[]{new Point(5,y),new Point(10,y-5),new Point(15,y),new Point(10,y+5)});
            TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(29,0,Math.Max(1,Width-29),Height),Enabled?ForeColor:Color.FromArgb(113,113,113),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
            if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(26,3,Math.Max(1,Width-28),Height-6),accent,BackColor);
        }
    }
    internal sealed class ShroudSurface : Panel {
        bool dormant;
        internal bool Dormant {get{return dormant;}set{if(dormant==value)return;dormant=value;Invalidate(true);}}
        internal ShroudSurface(){DoubleBuffered=true;BackColor=ShroudTheme.Panel;}
        protected override void OnPaintBackground(PaintEventArgs e) {
            if(Width<2 || Height<2)return;
            using(var b=new LinearGradientBrush(ClientRectangle,Dormant?ShroudTheme.Gray(ShroudTheme.PanelLight):ShroudTheme.PanelLight,Dormant?ShroudTheme.Gray(ShroudTheme.Panel):ShroudTheme.Panel,90))e.Graphics.FillRectangle(b,ClientRectangle);
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);using(var p=new Pen(Dormant?Color.FromArgb(99,99,99):Color.FromArgb(83,103,109)))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);
            using(var p=new Pen(Dormant?ShroudTheme.Gray(ShroudTheme.Gold):ShroudTheme.Gold)){int n=12;e.Graphics.DrawLine(p,0,n,0,0);e.Graphics.DrawLine(p,0,0,n,0);e.Graphics.DrawLine(p,Width-1,Height-n-1,Width-1,Height-1);e.Graphics.DrawLine(p,Width-1,Height-1,Width-n-1,Height-1);}
        }
    }
    internal sealed class ShroudSlider : Control {
        int current;
        internal int Minimum=-150,Maximum=150;
        internal event EventHandler ValueChanged;
        internal int Value {get{return current;}set{int v=Math.Max(Minimum,Math.Min(Maximum,value));if(v==current)return;current=v;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
        internal ShroudSlider(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Selectable,true);TabStop=true;Cursor=Cursors.Hand;BackColor=ShroudTheme.Panel;AccessibleRole=AccessibleRole.Slider;Height=48;}
        void SetFromX(int x){Value=Minimum+(int)Math.Round((Math.Max(12,Math.Min(Width-12,x))-12)*(Maximum-Minimum)/(double)Math.Max(1,Width-24));}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();Capture=true;SetFromX(e.X);}}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture && e.Button==MouseButtons.Left)SetFromX(e.X);}
        protected override void OnMouseUp(MouseEventArgs e){Capture=false;base.OnMouseUp(e);}
        protected override bool IsInputKey(Keys key){return (key&Keys.KeyCode)==Keys.Left || (key&Keys.KeyCode)==Keys.Right || (key&Keys.KeyCode)==Keys.Home || (key&Keys.KeyCode)==Keys.End || base.IsInputKey(key);}
        protected override void OnKeyDown(KeyEventArgs e) {
            int step=e.Shift?10:1;
            if(e.KeyCode==Keys.Left)Value-=step;else if(e.KeyCode==Keys.Right)Value+=step;else if(e.KeyCode==Keys.Home)Value=Minimum;else if(e.KeyCode==Keys.End)Value=Maximum;else{base.OnKeyDown(e);return;}e.Handled=true;
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            int y=Height/2,x=12+(int)Math.Round((Value-Minimum)*(Width-24)/(double)(Maximum-Minimum));
            int zero=12+(int)Math.Round(-Minimum*(Width-24)/(double)(Maximum-Minimum));
            using(var p=new Pen(Enabled?Color.FromArgb(70,97,110):Color.FromArgb(90,90,90),3))e.Graphics.DrawLine(p,12,y,Width-12,y);
            using(var p=new Pen(Enabled?ShroudTheme.Gold:Color.FromArgb(150,150,150),3))e.Graphics.DrawLine(p,zero,y,x,y);
            using(var p=new Pen(Enabled?ShroudTheme.Muted:ShroudTheme.Gray(ShroudTheme.Muted)))e.Graphics.DrawLine(p,zero,y-5,zero,y+5);
            Point[] diamond={new Point(x,y-9),new Point(x+7,y),new Point(x,y+9),new Point(x-7,y)};
            using(var b=new SolidBrush(!Enabled?ShroudTheme.Gray(ShroudTheme.Muted):Focused?ShroudTheme.Text:ShroudTheme.Gold))e.Graphics.FillPolygon(b,diamond);
            using(var p=new Pen(Enabled?ShroudTheme.Text:ShroudTheme.Gray(ShroudTheme.Text)))e.Graphics.DrawPolygon(p,diamond);
        }
        protected override AccessibleObject CreateAccessibilityInstance(){return new SliderAccessibility(this);}
        sealed class SliderAccessibility : ControlAccessibleObject {
            readonly ShroudSlider slider;
            internal SliderAccessibility(ShroudSlider s):base(s){slider=s;}
            public override AccessibleRole Role{get{return AccessibleRole.Slider;}}
            public override string Value {get{return (slider.Value/100f).ToString("0.00",CultureInfo.InvariantCulture);}set{if(slider.Enabled)slider.Value=NumericInput.Parse(value,slider.Value,slider.Minimum,slider.Maximum);}}
        }
    }
    internal sealed class CameraPanel : Form {
        CameraSession session;
        readonly ShroudSurface editor=new ShroudSurface {Dock=DockStyle.Fill,Padding=new Padding(22,16,22,16),Margin=Padding.Empty};
        bool? profilePaletteEnabled;
        Configurations configurations=Configurations.Defaults();
        Preferences preferences {get{return configurations.Current;}}
        readonly ShroudSlider horizontal=new ShroudSlider {Dock=DockStyle.Fill,AccessibleName="Décalage horizontal"};
        readonly ShroudSlider height=new ShroudSlider {Minimum=-75,Maximum=75,Dock=DockStyle.Fill,AccessibleName="Hauteur supplémentaire"};
        readonly Label horizontalLabel=TextLabel("DÉCALAGE HORIZONTAL"),heightLabel=TextLabel("HAUTEUR SUPPLÉMENTAIRE");
        readonly TextBox horizontalValue=ValueBox("Valeur du décalage horizontal"),heightValue=ValueBox("Valeur de la hauteur supplémentaire");
        readonly ShroudButton swap=Button("CHANGER D'ÉPAULE",210);
        readonly Label profileTitle=TextLabel(""),description=TextLabel(""),profileNote=TextLabel("");
        readonly Label status=TextLabel("Non connecté · aucun réglage appliqué");
        readonly CheckBox profileEnabled=Check("Activer ce profil",true);
        readonly Label saveHint=TextLabel("Sauvegarde automatique des réglages");
        readonly ShroudButton connect=Button("CONNECTER AU JEU",185),restore=Button("RESTAURER LE JEU",185);
        readonly ShroudButton[] tabs={Button("EXPLORATION",150),Button("ARC / BAGUETTE",150),Button("BÂTON",150)};
        readonly ShroudButton[] slots={Button("1",28),Button("2",28),Button("3",28)};
        readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer {Interval=750};
        readonly System.Windows.Forms.Timer saveTimer=new System.Windows.Forms.Timer {Interval=400};
        readonly ComboBox languageChoice=new ComboBox {DropDownStyle=ComboBoxStyle.DropDownList,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=24,FlatStyle=FlatStyle.Flat,BackColor=ShroudTheme.PanelLight,ForeColor=ShroudTheme.Text,Dock=DockStyle.Fill,Margin=new Padding(0,2,0,0)};
        string statusKey="Non connecté · aucun réglage appliqué",saveKey="Sauvegarde automatique des réglages";
        int selected;bool updating;
        static string SettingsFile{get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"shoulder-camera.json");}}
        static Label TextLabel(string text){return new BufferedLabel {Text=text,Tag=text,ForeColor=ShroudTheme.Text,BackColor=Color.Transparent,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};}
        static TextBox ValueBox(string name){return new TextBox {AccessibleName=name,TextAlign=HorizontalAlignment.Center,BackColor=ShroudTheme.PanelLight,ForeColor=ShroudTheme.Gold,BorderStyle=BorderStyle.FixedSingle,Dock=DockStyle.Fill,MaxLength=32};}
        static Control ValueRow(Label caption,TextBox value){
            var row=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty,BackColor=ShroudTheme.Panel};
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,84));
            caption.BackColor=ShroudTheme.Panel;row.Controls.Add(caption,0,0);row.Controls.Add(value,1,0);return row;
        }
        void BindValue(TextBox box,ShroudSlider slider){
            EventHandler commit=delegate{slider.Value=NumericInput.Parse(box.Text,slider.Value,slider.Minimum,slider.Maximum);box.Text=NumericInput.Format(slider.Value);};
            box.Leave+=commit;
            box.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Enter){commit(sender,EventArgs.Empty);box.SelectAll();e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Escape){box.Text=NumericInput.Format(slider.Value);box.SelectAll();e.SuppressKeyPress=true;}};
            box.Enter+=delegate{box.SelectAll();};
        }
        static ShroudButton Button(string text,int width){return new ShroudButton {Text=text,Tag=text,Width=width+10,Height=42,Margin=new Padding(0,0,10,0)};}
        static CheckBox Check(string text,bool enabled){return new ShroudCheckBox {Text=text,Tag=text,Enabled=enabled,ForeColor=ShroudTheme.Text,BackColor=ShroudTheme.Background,Margin=new Padding(0,5,0,0)};}
        ProfilePreferences Current {get{return selected==0?preferences.exploration:selected==1?preferences.bow:preferences.staff;}}
        internal CameraPanel() {
            Text="Shoulder Camera · 0.3.1";ClientSize=new Size(1000,710);MinimumSize=new Size(1000,710);
            BackColor=ShroudTheme.Background;ForeColor=ShroudTheme.Text;Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;StartPosition=FormStartPosition.CenterScreen;
            var outer=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=1,RowCount=5,BackColor=Color.Transparent};
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));outer.RowStyles.Add(new RowStyle(SizeType.Absolute,72));outer.RowStyles.Add(new RowStyle(SizeType.Absolute,54));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent,100));outer.RowStyles.Add(new RowStyle(SizeType.Absolute,54));outer.RowStyles.Add(new RowStyle(SizeType.Absolute,52));Controls.Add(outer);
            var heading=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2,RowCount=2};heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,205));heading.RowStyles.Add(new RowStyle(SizeType.Absolute,40));heading.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            var languageBox=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};languageBox.RowStyles.Add(new RowStyle(SizeType.Absolute,22));languageBox.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            languageChoice.DrawItem+=delegate(object sender,DrawItemEventArgs e){if(e.Index<0)return;bool focus=(e.State&DrawItemState.Selected)!=0;using(var brush=new SolidBrush(focus?ShroudTheme.PanelLight:ShroudTheme.Panel))e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,Localizer.Names[e.Index],languageChoice.Font,new Rectangle(e.Bounds.X+6,e.Bounds.Y,e.Bounds.Width-6,e.Bounds.Height),focus?ShroudTheme.Gold:ShroudTheme.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);};
            var languageLabel=TextLabel("Langue");languageLabel.Font=new Font("Segoe UI",8.5f);languageLabel.ForeColor=ShroudTheme.Muted;languageBox.Controls.Add(languageLabel,0,0);languageBox.Controls.Add(languageChoice,0,1);languageChoice.Items.AddRange(Localizer.Names);heading.Controls.Add(languageBox,1,0);heading.SetRowSpan(languageBox,2);
            var title=TextLabel("SHOULDER CAMERA");title.Font=new Font("Georgia",21);title.ForeColor=ShroudTheme.Gold;heading.Controls.Add(title,0,0);
            var sub=TextLabel("ENSHROUDED   /   RÉGLAGES DE CAMÉRA");sub.Font=new Font("Segoe UI",9);sub.ForeColor=ShroudTheme.Muted;heading.Controls.Add(sub,0,1);outer.Controls.Add(heading,0,0);
            var toolbar=new FlowLayoutPanel {Dock=DockStyle.Fill};toolbar.Controls.Add(connect);
            saveHint.Dock=DockStyle.None;saveHint.Size=new Size(360,42);saveHint.Font=new Font("Segoe UI",9);saveHint.ForeColor=ShroudTheme.Muted;toolbar.Controls.Add(saveHint);outer.Controls.Add(toolbar,0,1);
            var content=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty};content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,190));content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,18));content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            var nav=new ShroudSurface {Dock=DockStyle.Fill,Padding=new Padding(14),Margin=Padding.Empty};
            var navLayout=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=6};navLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));navLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
            var navHeader=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=4,RowCount=1,Margin=Padding.Empty};navHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,65));
            var navHeading=TextLabel("PROFILS");navHeading.Font=new Font("Segoe UI",9);navHeading.ForeColor=ShroudTheme.Gold;navHeading.Dock=DockStyle.None;navHeading.Size=new Size(65,30);navHeading.Margin=new Padding(0,1,0,0);navHeader.Controls.Add(navHeading,0,0);
            for(int i=0;i<3;i++){int index=i;navHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,29));slots[i].AccessibleName="Configuration "+(i+1);slots[i].Font=new Font("Segoe UI",9,FontStyle.Bold);slots[i].Dock=DockStyle.None;slots[i].Anchor=AnchorStyles.Top|AnchorStyles.Left;slots[i].Size=new Size(27,30);slots[i].Margin=new Padding(1,1,1,5);navHeader.Controls.Add(slots[i],i+1,0);slots[i].Click+=delegate{SelectConfiguration(index);};}
            navLayout.Controls.Add(navHeader,0,0);
            for(int i=0;i<3;i++){int index=i;tabs[i].Dock=DockStyle.Fill;tabs[i].Margin=new Padding(0,0,0,10);navLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,70));navLayout.Controls.Add(tabs[i],0,i+1);tabs[i].Click+=delegate{SelectProfile(index);};}
            navLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));navLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,90));
            var navHint=TextLabel("Le jeu passe automatiquement d'un profil à l'autre selon votre action.");navHint.Font=new Font("Segoe UI",9);navHint.ForeColor=ShroudTheme.Muted;navLayout.Controls.Add(navHint,0,5);nav.Controls.Add(navLayout);content.Controls.Add(nav,0,0);
            var fields=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=9};fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            foreach(int n in new int[]{38,38,32,36,44,36,44,56})fields.RowStyles.Add(new RowStyle(SizeType.Absolute,n));fields.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            profileTitle.Font=new Font("Georgia",17);profileTitle.ForeColor=ShroudTheme.Gold;description.ForeColor=ShroudTheme.Muted;description.Font=new Font("Segoe UI",9);
            var profileHeader=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};
            profileHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));profileHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,190));
            profileEnabled.BackColor=ShroudTheme.Panel;profileEnabled.Width=190;profileEnabled.Margin=new Padding(0,1,0,0);profileHeader.Controls.Add(profileTitle,0,0);profileHeader.Controls.Add(profileEnabled,1,0);
            fields.Controls.Add(profileHeader,0,0);fields.Controls.Add(description,0,1);
            var entryHint=TextLabel("Cliquez sur une valeur pour la saisir · Entrée pour valider");entryHint.Font=new Font("Segoe UI",8.5f);entryHint.ForeColor=ShroudTheme.Muted;fields.Controls.Add(entryHint,0,2);
            fields.Controls.Add(ValueRow(horizontalLabel,horizontalValue),0,3);fields.Controls.Add(horizontal,0,4);fields.Controls.Add(ValueRow(heightLabel,heightValue),0,5);fields.Controls.Add(height,0,6);
            var actions=new FlowLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(0,4,0,0),Margin=Padding.Empty};actions.Controls.Add(swap);fields.Controls.Add(actions,0,7);
            profileNote.Font=new Font("Segoe UI",9);profileNote.ForeColor=ShroudTheme.Muted;fields.Controls.Add(profileNote,0,8);editor.Controls.Add(fields);content.Controls.Add(editor,2,0);outer.Controls.Add(content,0,2);
            status.ForeColor=ShroudTheme.Muted;status.Font=new Font("Segoe UI",9);outer.Controls.Add(status,0,3);
            var footer=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,245));restore.Width=240;connect.Width=225;
            footer.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            var footnote=TextLabel("Distance et FOV : dans le jeu.\nFermer le panneau restaure la caméra.");footnote.Font=new Font("Segoe UI",8.5f);footnote.ForeColor=ShroudTheme.Muted;footer.Controls.Add(footnote,0,0);
            var bottom=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};restore.Margin=Padding.Empty;bottom.Controls.Add(restore);footer.Controls.Add(bottom,1,0);outer.Controls.Add(footer,0,4);
            tabs[1].Font=new Font("Segoe UI",9,FontStyle.Bold);BindValue(horizontalValue,horizontal);BindValue(heightValue,height);LoadPreferences();ApplyLanguage();UpdateSlotButtons();
            languageChoice.SelectedIndexChanged+=delegate{if(updating)return;configurations.language=Localizer.Codes[languageChoice.SelectedIndex];ApplyLanguage();ScheduleSave();};
            connect.Click+=async delegate {
                connect.Enabled=false;SetStatus("Recherche des réglages de caméra…",ShroudTheme.Muted);
                try {CameraSession result=await Task.Run(()=>CameraSession.Connect());if(IsDisposed){result.Dispose();return;}session=result;Apply();}
                catch(Exception error){if(!IsDisposed){SetStatus(error.Message,Color.Salmon);connect.Enabled=true;}}
            };
            horizontal.ValueChanged+=delegate{if(updating)return;Current.horizontal=horizontal.Value;PreferencesChanged();};
            height.ValueChanged+=delegate{if(updating)return;Current.height=height.Value;PreferencesChanged();};
            profileEnabled.CheckedChanged+=delegate{if(updating)return;Current.enabled=profileEnabled.Checked;PreferencesChanged();};
            swap.Click+=delegate{horizontal.Value=-horizontal.Value;};restore.Click+=delegate{Restore();};
            saveTimer.Tick+=delegate{saveTimer.Stop();try{SavePreferences();SetSaveHint("Réglages enregistrés automatiquement",ShroudTheme.Muted);}catch{SetSaveHint("Échec de la sauvegarde automatique",Color.Salmon);}};
            timer.Tick+=delegate{if(session!=null && !session.IsAlive){session.Dispose();session=null;connect.Enabled=true;SetStatus("Jeu fermé · reconnectez après son prochain lancement.",ShroudTheme.Muted);}};timer.Start();
            FormClosing+=delegate(object sender,FormClosingEventArgs e) {
                if(session!=null)try{session.Restore();session.Dispose();session=null;}catch(Exception error){e.Cancel=true;SetStatus(error.Message,Color.Salmon);MessageBox.Show(this,Localizer.Get("Fermez le jeu pour supprimer les réglages temporaires, puis fermez le panneau."),Localizer.Get("Restauration incomplète"),MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
                try{SavePreferences();}catch{}
            };
            FormClosed+=delegate{timer.Dispose();saveTimer.Dispose();};
        }
        void SetStatus(string text,Color color){statusKey=text;string translated=Localizer.Get(text);if(status.Text!=translated)status.Text=translated;if(status.ForeColor!=color)status.ForeColor=color;}
        void SetSaveHint(string key,Color color){saveKey=key;saveHint.Text=Localizer.Get(key);saveHint.ForeColor=color;}
        void TranslateControls(Control parent){foreach(Control child in parent.Controls){string key=child.Tag as string;if(!string.IsNullOrEmpty(key))child.Text=Localizer.Get(key);TranslateControls(child);}}
        internal void ApplyLanguage(){
            updating=true;Localizer.Language=Localizer.Normalize(configurations.language);languageChoice.SelectedIndex=Array.IndexOf(Localizer.Codes,Localizer.Language);TranslateControls(this);
            languageChoice.AccessibleName=Localizer.Get("Langue");horizontal.AccessibleName=Localizer.Get("Décalage horizontal");height.AccessibleName=Localizer.Get("Hauteur supplémentaire");
            horizontalValue.AccessibleName=Localizer.Get("Valeur du décalage horizontal");heightValue.AccessibleName=Localizer.Get("Valeur de la hauteur supplémentaire");
            for(int i=0;i<3;i++)slots[i].AccessibleName=Localizer.Get("Configuration")+" "+(i+1);
            updating=false;SelectProfile(selected);SetStatus(statusKey,status.ForeColor);SetSaveHint(saveKey,saveHint.ForeColor);Invalidate(true);
        }
        internal void PreviewLanguage(string code){configurations.language=Localizer.Normalize(code);ApplyLanguage();}
        void UpdateSlotButtons(){for(int i=0;i<3;i++)slots[i].Selected=i==configurations.selectedSlot;}
        void SelectConfiguration(int index){
            if(index==configurations.selectedSlot)return;
            configurations.selectedSlot=Math.Max(0,Math.Min(2,index));UpdateSlotButtons();SelectProfile(selected);PreferencesChanged();
        }
        internal void SelectProfile(int index) {
            selected=Math.Max(0,Math.Min(2,index));updating=true;for(int i=0;i<tabs.Length;i++)tabs[i].Selected=i==selected;
            profileTitle.Text=Localizer.Get(new string[]{"EXPLORATION","ARC ET BAGUETTE","VISÉE AU BÂTON"}[selected]);
            description.Text=Localizer.Get(new string[]{"Marche, course, attente et accroupissement.","Vues de visée partagées par l'arc et la baguette.","Visée et canalisation du bâton, debout ou accroupi."}[selected]);
            horizontal.Value=Current.horizontal;height.Value=Current.height;profileEnabled.Checked=Current.enabled;updating=false;UpdateLabels();
        }
        void UpdateLabels() {
            tabs[0].ProfileActive=preferences.exploration.enabled;tabs[1].ProfileActive=preferences.bow.enabled;tabs[2].ProfileActive=preferences.staff.enabled;
            string x=NumericInput.Format(horizontal.Value),y=NumericInput.Format(height.Value);
            if(horizontalValue.Text!=x)horizontalValue.Text=x;if(heightValue.Text!=y)heightValue.Text=y;
            bool enabled=Current.enabled;
            horizontal.Enabled=height.Enabled=horizontalValue.Enabled=heightValue.Enabled=swap.Enabled=enabled;
            if(profilePaletteEnabled!=enabled){
                editor.SuspendLayout();ApplyProfilePalette(editor,enabled);editor.Dormant=!enabled;profilePaletteEnabled=enabled;editor.ResumeLayout(true);editor.Invalidate(true);
            }
            profileNote.Text=Localizer.Get(Current.enabled?"Profil personnalisé · les autres profils gardent leurs propres valeurs.":"Profil natif · les réglages d'origine du jeu sont conservés.");
        }
        void ApplyProfilePalette(Control control,bool enabled){
            Color background=control is TextBox?ShroudTheme.PanelLight:ShroudTheme.Panel;
            Color foreground=control==profileTitle || control is TextBox?ShroudTheme.Gold:control is Label && control!=horizontalLabel && control!=heightLabel?ShroudTheme.Muted:ShroudTheme.Text;
            control.BackColor=enabled?background:ShroudTheme.Gray(background);
            control.ForeColor=enabled?foreground:ShroudTheme.Gray(foreground);
            foreach(Control child in control.Controls)ApplyProfilePalette(child,enabled);
        }
        void PreferencesChanged(){
            UpdateLabels();ScheduleSave();Apply();
        }
        void ScheduleSave(){saveTimer.Stop();SetSaveHint("Enregistrement des réglages…",ShroudTheme.Muted);saveTimer.Start();}
        void LoadPreferences() {try{if(File.Exists(SettingsFile))using(var file=File.OpenRead(SettingsFile))configurations=Configurations.Read(file);}catch{SetStatus("Préférences illisibles · réglages par défaut utilisés.",Color.Salmon);}}
        void SavePreferences() {
            string temporary=SettingsFile+".tmp";using(var file=File.Create(temporary))configurations.Write(file);
            if(File.Exists(SettingsFile))File.Replace(temporary,SettingsFile,null);else File.Move(temporary,SettingsFile);
        }
        void Apply() {
            if(updating || session==null)return;
            try{session.ApplyProfiles(preferences.exploration.ToProfile(),preferences.bow.ToProfile(),preferences.staff.ToProfile());SetStatus(preferences.exploration.enabled || preferences.bow.enabled || preferences.staff.enabled?"Jeu connecté · les profils cochés sont appliqués automatiquement.":"Jeu connecté · tous les profils utilisent la caméra d'origine.",ShroudTheme.Blue);}
            catch(Exception error){Restore();SetStatus(error.Message,Color.Salmon);}
        }
        void Restore() {if(session==null)return;try{session.Restore();session.Dispose();session=null;connect.Enabled=true;SetStatus("Caméra d'origine restaurée · reconnectez pour réappliquer vos profils.",ShroudTheme.Muted);}catch(Exception error){SetStatus(error.Message,Color.Salmon);}}
    }
}






