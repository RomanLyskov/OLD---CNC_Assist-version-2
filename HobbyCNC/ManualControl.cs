﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace CNC_Assist
{
    public partial class ManualControl : Form
    {


        public ManualControl()
        {
            InitializeComponent();
        }

        private void KeyInfo_Load(object sender, EventArgs e)
        {

        }

        private void button8_MouseDown(object sender, MouseEventArgs e)
        {
            button8.BackColor = Color.DarkGreen;
            ControllerPlanetCNC.StartManualMove("-", "+", "0", (int)numericUpDown1.Value);
        }

        private void button8_MouseUp(object sender, MouseEventArgs e)
        {
            button8.BackColor = Color.FromName("Control");
            ControllerPlanetCNC.StopManualMove();
        }

        private void button2_MouseUp(object sender, MouseEventArgs e)
        {
            button2.BackColor = Color.FromName("Control");
            ControllerPlanetCNC.StopManualMove();
        }

        private void button2_MouseDown(object sender, MouseEventArgs e)
        {
            button2.BackColor = Color.DarkGreen;
            ControllerPlanetCNC.StartManualMove("0", "+", "0", (int)numericUpDown1.Value);
        }

        private void button10_MouseDown(object sender, MouseEventArgs e)
        {
            button10.BackColor = Color.DarkGreen;
            ControllerPlanetCNC.StartManualMove("+", "+", "0", (int)numericUpDown1.Value);
        }

        private void button10_MouseUp(object sender, MouseEventArgs e)
        {
            button10.BackColor = Color.FromName("Control");
            ControllerPlanetCNC.StopManualMove();
        }

        private void button7_MouseDown(object sender, MouseEventArgs e)
        {
            button7.BackColor = Color.DarkGreen;
            ControllerPlanetCNC.StartManualMove("+", "0", "0", (int)numericUpDown1.Value);
        }

        private void button7_MouseUp(object sender, MouseEventArgs e)
        {
            button7.BackColor = Color.FromName("Control");
            ControllerPlanetCNC.StopManualMove();
        }

        private void button11_MouseDown(object sender, MouseEventArgs e)
        {
            button11.BackColor = Color.DarkGreen;
            ControllerPlanetCNC.StartManualMove("+", "-", "0", (int)numericUpDown1.Value);
        }

        private void button11_MouseUp(object sender, MouseEventArgs e)
        {
            button11.BackColor = Color.FromName("Control");
            ControllerPlanetCNC.StopManualMove();
        }

        private void button3_MouseDown(object sender, MouseEventArgs e)
        {
            button3.BackColor = Color.DarkGreen;
            ControllerPlanetCNC.StartManualMove("0", "-", "0", (int)numericUpDown1.Value);
        }

        private void button3_MouseUp(object sender, MouseEventArgs e)
        {
            button3.BackColor = Color.FromName("Control");
            ControllerPlanetCNC.StopManualMove();
        }

        private void button9_MouseDown(object sender, MouseEventArgs e)
        {
            button9.BackColor = Color.DarkGreen;
            ControllerPlanetCNC.StartManualMove("-", "-", "0", (int)numericUpDown1.Value);
        }

        private void button9_MouseUp(object sender, MouseEventArgs e)
        {
            button9.BackColor = Color.FromName("Control");
            ControllerPlanetCNC.StopManualMove();
        }

        private void button6_MouseDown(object sender, MouseEventArgs e)
        {
            button6.BackColor = Color.DarkGreen;
            ControllerPlanetCNC.StartManualMove("-", "0", "0", (int)numericUpDown1.Value);
        }

        private void button6_MouseUp(object sender, MouseEventArgs e)
        {
            button6.BackColor = Color.FromName("Control");
            ControllerPlanetCNC.StopManualMove();
        }

        private void button4_MouseDown(object sender, MouseEventArgs e)
        {
            button4.BackColor = Color.DarkGreen;
            ControllerPlanetCNC.StartManualMove("0", "0", "+", (int)numericUpDown1.Value);
        }

        private void button4_MouseUp(object sender, MouseEventArgs e)
        {
            button4.BackColor = Color.FromName("Control");
            ControllerPlanetCNC.StopManualMove();
        }

        private void button5_MouseDown(object sender, MouseEventArgs e)
        {
            button5.BackColor = Color.DarkGreen;
            ControllerPlanetCNC.StartManualMove("0", "0", "-", (int)numericUpDown1.Value);
        }

        private void button5_MouseUp(object sender, MouseEventArgs e)
        {
            button5.BackColor = Color.FromName("Control");
            ControllerPlanetCNC.StopManualMove();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            numericUpDown1.Value = trackBar1.Value;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (ControllerPlanetCNC.IsConnectedToController)
            {

                groupBox1.Enabled = ControllerPlanetCNC.IsAvailability;
   
            }
            else
            {
                groupBox1.Enabled = false;
            }
        }

    }
}
