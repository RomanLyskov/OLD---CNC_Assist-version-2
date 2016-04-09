﻿using System;
using System.Drawing;
using System.Windows.Forms;
using CNC_Assist.PlanetCNC;
using CNC_Assist.Properties;

namespace CNC_Assist
{
    public partial class GuiPanelTaskControl : UserControl
    {
        /// <summary>
        /// Для сравнения с датой загрузки из файла G-кода, и последующего перезаполнения
        /// </summary>
        private DateTime _dateGetLastNewDataCode;

        /// <summary>
        /// текущая выполняемая строка
        /// </summary>
        private static int _nowPos;   
        /// <summary>
        /// конечная строка данных для выполнения
        /// </summary>
        private static int _endPos; 

        public GuiPanelTaskControl()
        {
            InitializeComponent();

            _nowPos = 0;
            _endPos = 0;
        }

        // выделение мышкой одной или нескольких строк g-кода
        private void listGkodeCommand_SelectedIndexChanged(object sender, EventArgs e)
        {
            DataLoader.SelectedRowStart = listGkodeCommand.SelectedIndex;
            DataLoader.SelectedRowStop  = listGkodeCommand.SelectedIndex + listGkodeCommand.SelectedItems.Count;
        }

        // флаг применять свою скорость движения
        private void checkBoxManualSpeed_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown1.Enabled = checkBoxManualSpeed.Checked;
            numericUpDown2.Enabled = checkBoxManualSpeed.Checked;
        }

        private void TaskTimer_Tick(object sender, EventArgs e)
        {
            textBoxNumberLine.Text = @"Выполнена: " + ControllerPlanetCNC.Info.NuberCompleatedInstruction.ToString();


            // проверим не обновились ли данные
            if (_dateGetLastNewDataCode != DataLoader.dateGetNewDataCode && DataLoader.status == DataLoader.eDataSetStatus.none)
            {
                listGkodeCommand.Items.Clear();
                foreach (DataRow val in DataLoader.DataRows)
                {
                    listGkodeCommand.Items.Add(@"№" + val.numberRow + " " + val.DataString);
                }
                _dateGetLastNewDataCode = DataLoader.dateGetNewDataCode;
            }

            if (ControllerPlanetCNC.StatusTThread == enumStatusThread.Off)
            {
                labelStatusTask.Image = Resources.ball_red;
                labelStatusTask.Text = @"Нет связи";
                buttonStartTask.Enabled = false;
                buttonPauseTask.Enabled = false;
                btStopTask.Enabled = false;
            }

            if (ControllerPlanetCNC.StatusTThread == enumStatusThread.Wait)
            {
                labelStatusTask.Image = Resources.ball_red;
                labelStatusTask.Text = @"Нет задания";
                buttonStartTask.Enabled = true;
                buttonPauseTask.Enabled = false;
                btStopTask.Enabled = false;
            }

            //if (Controller.StatusTThread == ETaskStatus.Start)
            //{
            //    labelStatusTask.Image = Resources.ball_yellow;
            //    labelStatusTask.Text = @"Запуск";
            //    buttonStartTask.Enabled = false;
            //    buttonPauseTask.Enabled = true;
            //    btStopTask.Enabled = true;
            //}

            //if (Controller.StatusTThread == ETaskStatus.Stop)
            //{
            //    labelStatusTask.Image = Resources.ball_yellow;
            //    labelStatusTask.Text = @"Остановка";
            //    buttonStartTask.Enabled = false;
            //    buttonPauseTask.Enabled = false;
            //    btStopTask.Enabled = false;
            //}

            if (ControllerPlanetCNC.StatusTThread == enumStatusThread.Work)
            {
                labelStatusTask.Image = Resources.ball_green;
                labelStatusTask.Text = @"ВЫПОЛНЕНИЕ";
                buttonStartTask.Enabled = false;
                buttonPauseTask.Enabled = true;
                btStopTask.Enabled = true;
            }

            if (ControllerPlanetCNC.StatusTThread == enumStatusThread.Pause)
            {
                labelStatusTask.Image = Resources.ball_yellow;
                labelStatusTask.Text = @"ПАУЗА";
                buttonStartTask.Enabled = false;
                buttonPauseTask.Enabled = true;
                btStopTask.Enabled = true;
            }

            if (!ControllerPlanetCNC.IsConnectedToController)
            {
                groupBoxManualSpeedGkode.Enabled = false;
                groupBoxWorking.Enabled = false;
                return;
            }

            groupBoxManualSpeedGkode.Enabled = true;
            groupBoxWorking.Enabled = true;
        }

        private void buttonStartTask_Click(object sender, EventArgs e)
        {

            ControllerPlanetCNC.TASK_CLEAR();

            ControllerPlanetCNC.TASK_SendStartData();

            // Определимся с границами
            _nowPos = listGkodeCommand.SelectedIndex;

            if (listGkodeCommand.SelectedItems.Count == 1)
            {
                _endPos = listGkodeCommand.Items.Count;
            }
            else
            {
                _endPos = listGkodeCommand.SelectedIndex + listGkodeCommand.SelectedItems.Count;
            }
            
            if (_nowPos >= _endPos)
            {
                return;
            }



            // При запуске передаем данные до точки, где нужно сменить инструмент, или сделать паузу

            
            bool needContinue = true;



            // для поиска различий при отправке данных
            DataRow dataRowOld = new DataRow(0, "");

            while (needContinue)
            {
                // сравним наличие изменений в данных, и проанализируем какие команды послать в контроллер
                DataRow dataRowNow = DataLoader.DataRows[_nowPos];
                if (_nowPos != 0) dataRowOld = DataLoader.DataRows[_nowPos - 1];

                // В случае наличия изменений, отправим новые данные
                //if (dataRowNow.Machine.SpindelON != dataRowOld.Machine.SpindelON || dataRowNow.Machine.SpeedSpindel != dataRowOld.Machine.SpeedSpindel)
                //{
                    ControllerPlanetCNC.AddBinaryDataToTask(BinaryData.pack_B5(dataRowNow.Machine.SpindelON, 2, BinaryData.TypeSignal.Hz, dataRowNow.Machine.SpeedSpindel));
                    //TODO: это нужно переделать!!!!! зафиксируем
                    PlanetCNC_Controller.LastStatus = dataRowNow;
              //  }


                if (dataRowNow.POS.X != dataRowOld.POS.X || dataRowNow.POS.Y != dataRowOld.POS.Y || dataRowNow.POS.Z != dataRowOld.POS.Z || dataRowNow.POS.Z != dataRowOld.POS.Z)
                {
                    //if (Controller.INFO.NuberCompleatedInstruction == 0)
                    //{
                    //    //если нет номера инструкции, то отправляем пока буфер не сильно занят
                    //    if (GlobalSetting.AppSetting.Controller == ControllerModel.PlanetCNC_MK1 && Controller.INFO.FreebuffSize < 4) return;
                    //    if (GlobalSetting.AppSetting.Controller == ControllerModel.PlanetCNC_MK2 && Controller.INFO.FreebuffSize < 230) return;
                    //}
                    //else
                    //{
                    //    //знаем номер инструкции, и будем отправлять пока не более 10 инструкций
                    //    if (_nowPos > (Controller.INFO.NuberCompleatedInstruction + GlobalSetting.ControllerSetting.MinBuffSize)) return;
                    //}

                    int speedToSend = dataRowNow.Machine.SpeedMaсhine;

                    if (checkBoxManualSpeed.Checked)
                    {
                        if (dataRowNow.Machine.NumGkode == 0) speedToSend = (int)numericUpDown1.Value;

                        if (dataRowNow.Machine.NumGkode == 1) speedToSend = (int)numericUpDown2.Value;
                    }

                    //координаты следующей точки
                    float pointX = (float)dataRowNow.POS.X;
                    float pointY = (float)dataRowNow.POS.Y;
                    float pointZ = (float)dataRowNow.POS.Z;

                    //добавление смещения G-кода
                    if (ControllerPlanetCNC.CorrectionPos.UseCorrection)
                    {
                        //// применение пропорций
                        //pointX *= Setting.koeffSizeX;
                        //pointY *= Setting.koeffSizeY;

                        //применение смещения
                        pointX += (float)ControllerPlanetCNC.CorrectionPos.DeltaX;
                        pointY += (float)ControllerPlanetCNC.CorrectionPos.DeltaY;

                        //применение матрицы поверхности детали
                        if (ControllerPlanetCNC.CorrectionPos.UseMatrix)
                        {
                            pointZ += ScanSurface.GetPosZ(pointX, pointY);
                        }

                        pointZ += (float)ControllerPlanetCNC.CorrectionPos.DeltaZ;

                    }

                    //отладка
                    ControllerPlanetCNC.AddBinaryDataToTask(BinaryData.pack_B5(dataRowNow.Machine.SpindelON, 2, BinaryData.TypeSignal.Hz, dataRowNow.Machine.SpeedSpindel));


                    ControllerPlanetCNC.AddBinaryDataToTask(BinaryData.pack_CA(ControllerPlanetCNC.Info.CalcPosPulse("X", (decimal)pointX),
                                                                    ControllerPlanetCNC.Info.CalcPosPulse("Y", (decimal)pointY),
                                                                    ControllerPlanetCNC.Info.CalcPosPulse("Z", (decimal)pointZ),
                                                                    ControllerPlanetCNC.Info.CalcPosPulse("A", dataRowNow.POS.A),
                                                                    speedToSend,
                                                                    dataRowNow.numberRow, dataRowNow.Machine.TimeOutPause));

                    //TODO: это нужно переделать!!!!! зафиксируем
                    PlanetCNC_Controller.LastStatus = dataRowNow;
                }

                _nowPos++;

                //TODO: так-же добавить прерывание, в случае смены инструмента/паузы
                
                // вариант 1 - выполняется до последней строчки
                needContinue = (_nowPos < _endPos);

                // вариант 2 - выполняется до смены инструмента, и активируется остановка
                if (dataRowNow.Tools.NeedChange)
                {
                    needContinue = false;
                    MessageBox.Show("Возникла необходимость смены инструмента № " + dataRowNow.Tools.NumberTools +
                                    ", с диамтром: " + dataRowNow.Tools.DiametrTools);

                }



            }


            // данные переданы для выполнения, запустим....

            ControllerPlanetCNC.TASK_SendStopData();

            ControllerPlanetCNC.TASK_START();








            //////TODO: НУЖНО ОСТАНОВИТЬ ПЕРЕДАЧУ ДАННЫХ, если будет смена инструмента, с последующей паузой
            //////if (dataRowNow.Tools.NeedChange)
            //////{
            //////    //timerTask.Enabled = false;
            //////    //_statusTask = ETaskStatus.Pause;
            //////    //Controller.TestAllowActions = false; //разблокируем
            //////    //MessageBox.Show(@"Для дальнейшей работы, установите инструмент № " + dataRowNow.Tools.NumberTools + ", диаметром " + dataRowNow.Tools.DiametrTools + ", после установки нового инструмента, нужно продолжить выполнение (сейчас включена пауза)");
            //////    //_nowPos++;                    
            //////    //timerTask.Enabled = true;

            //////    return;
            //////}

            //////TODO: пока не работает
            //////if (dataRowNow.Extra.NeedPause)
            //////{
            //////    //MessageBox.Show(@"Выполняется пауза длительностью " + dataRowNow.Extra.timeoutMsec + @" мс.", "",
            //////    //    MessageBoxButtons.OK);
            //////    //System.Threading.Thread.Sleep(dataRowNow.Extra.timeoutMsec);
            //////}

            //////Сравнить, и установить в случае необходимости
            //////1) Шпиндель и скорость работы
            //////2) Выполнить движение с необходимой скоростью

            //////if (dataRowNow.Machine != dataRowOld.Machine)







        }

        private void buttonPauseTask_Click(object sender, EventArgs e)
        {
            ControllerPlanetCNC.TASK_PAUSE();
        }

        private void btStopTask_Click(object sender, EventArgs e)
        {
            ControllerPlanetCNC.TASK_STOP();
        }

        /// <summary>
        /// Предыдущее значение
        /// </summary>
        private void timer1_Tick(object sender, EventArgs e)
        {

        }

        private void listGkodeCommand_DrawItem(object sender, DrawItemEventArgs e)
        {
            // Перерисовываем фон всех элементов ListBox.  
            e.DrawBackground();

            // Создаем объект Brush.  
            Brush myBrush = Brushes.Black;

            // Определяем номер текущего элемента  
            //switch (e.Index)
            //{
            //    case 0:
            //        myBrush = Brushes.Red;
            //        break;
            //    case 1:
            //        myBrush = Brushes.Green;
            //        break;
            //    case 2:
            //        myBrush = Brushes.Blue;
            //        break;
            //    default: myBrush = Brushes.Yellow;
            //        break;
            //}

            //Если необходимо, закрашиваем фон   
            //активного элемента в новый цвет  
            //e.Graphics.FillRectangle(myBrush, e.Bounds);  


            try
            {
                // Перерисовываем текст текущего элемента  
                e.Graphics.DrawString(((ListBox)sender).Items[e.Index].ToString(), e.Font, myBrush, e.Bounds, StringFormat.GenericDefault);
                //выделение другим цветом в той-же строке
                //e.Graphics.DrawString(" 123", e.Font, Brushes.Blue, e.Bounds, StringFormat.GenericDefault);

                // Если ListBox в фокусе, рисуем прямоугольник   
                //вокруг активного элемента.  
                e.DrawFocusRectangle();
            }
            catch (Exception)
            {
                
              //  throw;
            }

            

        }
    }
}