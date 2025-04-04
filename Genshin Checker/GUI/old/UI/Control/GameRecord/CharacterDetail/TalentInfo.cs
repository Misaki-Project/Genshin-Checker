﻿using Genshin_Checker.Core.General.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Genshin_Checker.UI.Control.GameRecord.CharacterDetail
{
    public partial class TalentInfo : UserControl
    {
        string iconlink = "";
        public TalentInfo(string talentIcon,string talentName, string talentLevel,bool ConstellationEnabled, string talentDescription)
        {
            InitializeComponent();
            label1.Text = talentName;
            label2.Text = talentLevel;
            if (ConstellationEnabled)
            {
                label2.ForeColor = Color.DarkTurquoise;
                label2.Font = new(label2.Font, FontStyle.Bold);
            }
            //label3.Text=talentDescription;
            iconlink = talentIcon;
        }

        private async void TalentInfo_Load(object sender, EventArgs e)
        {
            pictureBox1.Image = DrawControl.BitmapInterpolation(await Core.WebRequest.ImageGetRequest(iconlink), pictureBox1.Width, pictureBox1.Height);
        }
    }
}
