using Enums;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
//using static SkinCancerPredictor.MLModel1;

namespace SkinCancerPredictor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty ImageLoadedProperty =
            DependencyProperty.Register("ImageLoaded", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public static readonly DependencyProperty ImageNotLoadedProperty =
            DependencyProperty.Register("ImageNotLoaded", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

        private int[] AvailableDistortionPercentages = new int[] { 5, 10, 15, 20, 30, 40, 50 };
        private bool ImageDistorted;

        //for border visibility
        public bool ImageNotLoaded
        {
            get { return (bool)GetValue(ImageNotLoadedProperty); }
            set { SetValue(ImageNotLoadedProperty, value); }
        }

        public bool ImageLoaded
        {
            get { return (bool)GetValue(ImageLoadedProperty); }
            set { SetValue(ImageLoadedProperty, value); }
        }

        public MainWindow()
        {
            InitializeComponent();
            InitPredictLabels();
            cbDistPercent.ItemsSource = AvailableDistortionPercentages;
            DataContext = this;
            ImageLoaded = false;
            ImageNotLoaded = true;
            ImageDistorted = false;
        }

        private void InitPredictLabels()
        {
            lblBcc.Content = Constants.DiagnoseDescriptionDictionary[DiagnoseCode.bcc];
            lblMel.Content = Constants.DiagnoseDescriptionDictionary[DiagnoseCode.mel];
            lblNv.Content = Constants.DiagnoseDescriptionDictionary[DiagnoseCode.nv];
            lblVasc.Content = Constants.DiagnoseDescriptionDictionary[DiagnoseCode.vasc];
        }

        private void btnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dialog.Multiselect = false;
            dialog.Filter = "Image files (*.jpg)|*.jpg|(*.png)|*.png";
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == true)
            {
                ResetPredictLabels();
                string selectedFileName = dialog.FileName;
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(selectedFileName);
                bitmap.EndInit();

                imgInput.Source = bitmap;
                lblImageUrl.Content = selectedFileName;
                cbDistPercent.SelectedIndex = -1;
                ImageDistorted = false;
                ImageLoaded = true;
                ImageNotLoaded = false;
            }
        }

        private void btnResetImage_Click(object sender, RoutedEventArgs e)
        {
            ResetAll();
        }

        private void ResetAll()
        {
            ImageDistorted = false;
            ImageLoaded = false;
            ImageNotLoaded = true;
            imgInput.Source = null;
            lblImageUrl.Content = string.Empty;
            cbDistPercent.SelectedIndex = -1;
            ResetPredictLabels();
        }

        private void btnPredict_Click(object sender, RoutedEventArgs e)
        {
            string tempDirectory = string.Empty;
            try
            {
                if (ImageDistorted)
                {
                    tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    string tempFileDirectory = Path.Combine(tempDirectory, "tmp.jpg");

                    Directory.CreateDirectory(tempDirectory);

                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imgInput.Source));
                    using (FileStream stream = new FileStream(tempFileDirectory, FileMode.Create))
                    {
                        encoder.Save(stream);
                    }

                    //ModelInput data = new ModelInput()
                    //{
                    //    ImageSource = tempFileDirectory,
                    //};

                    Mouse.OverrideCursor = Cursors.Wait;
                    //ModelOutput predictionResult = Predict(data);
                    Mouse.OverrideCursor = Cursors.Arrow;

                    //Dictionary<DiagnoseCode, decimal> scoresWithLabels = GetScoresWithLabels(predictionResult);
                    //ShowPredictLabelsScores(scoresWithLabels, predictionResult.Prediction);
                }
                else
                {
                    //PredictFile(lblImageUrl.Content?.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                ResetAll();
            }
            finally
            {
                if (ImageDistorted && Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
        }

        //private void PredictFile(string? filePath)
        //{
        //    if (string.IsNullOrEmpty(filePath) || imgInput.Source == null)
        //    {
        //        return;
        //    }

        //    ModelInput data = new ModelInput()
        //    {
        //        ImageSource = filePath,
        //    };

        //    Mouse.OverrideCursor = Cursors.Wait;
        //    ModelOutput predictionResult = Predict(data);
        //    Mouse.OverrideCursor = Cursors.Arrow;

        //    Dictionary<DiagnoseCode, decimal> scoresWithLabels = GetScoresWithLabels(predictionResult);
        //    ShowPredictLabelsScores(scoresWithLabels, predictionResult.Prediction);
        //}

        private void ResetPredictLabels()
        {
            lblBccScore.Content = string.Empty;
            lblMelScore.Content = string.Empty;
            lblNvScore.Content = string.Empty;
            lblVascScore.Content = string.Empty;

            lblBcc.FontWeight = FontWeights.Normal;
            lblMel.FontWeight = FontWeights.Normal;
            lblNv.FontWeight = FontWeights.Normal;
            lblVasc.FontWeight = FontWeights.Normal;

            lblBccScore.FontWeight = FontWeights.Normal;
            lblMelScore.FontWeight = FontWeights.Normal;
            lblNvScore.FontWeight = FontWeights.Normal;
            lblVascScore.FontWeight = FontWeights.Normal;
        }

        private void SetPredictScoreLabel(Label diagnoseKindLabel, Label predictScoreLabel, KeyValuePair<DiagnoseCode, decimal> score, string prediction)
        {
            if (diagnoseKindLabel == null || predictScoreLabel == null || string.IsNullOrEmpty(prediction))
            {
                return;
            }

            predictScoreLabel.Content = $"{score.Value * 100}%";

            if (string.Equals(prediction, score.Key.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                diagnoseKindLabel.FontWeight = FontWeights.Bold;
                predictScoreLabel.FontWeight = FontWeights.Bold;
            }
        }

        private void ShowPredictLabelsScores(Dictionary<DiagnoseCode, decimal> scoresWithLabels, string prediction)
        {
            foreach (KeyValuePair<DiagnoseCode, decimal> score in scoresWithLabels)
            {
                switch (score.Key)
                {
                    //add case when new diagnose types are added to display
                    case DiagnoseCode.bcc:
                        SetPredictScoreLabel(lblBcc, lblBccScore, score, prediction);
                        break;
                    case DiagnoseCode.mel:
                        SetPredictScoreLabel(lblMel, lblMelScore, score, prediction);
                        break;
                    case DiagnoseCode.nv:
                        SetPredictScoreLabel(lblNv, lblNvScore, score, prediction);
                        break;
                    case DiagnoseCode.vasc:
                        SetPredictScoreLabel(lblVasc, lblVascScore, score, prediction);
                        break;
                    default:
                        throw new Exception($"Cannot find diagnose code for {score.Key}");
                }
            }
        }

        private void btnDistort_Click(object sender, RoutedEventArgs e)
        {
            if (cbDistPercent.SelectedIndex <= -1)
            {
                MessageBox.Show("Select distortion percentage");
                return;
            }

            int distPercent = (int)cbDistPercent.SelectedValue;

            try
            {
                using (Bitmap sourceBtm = new Bitmap(lblImageUrl.Content?.ToString()))
                {
                    Bitmap distortedBtm = GetDistortedBitmap(sourceBtm, distPercent);
                    ResetPredictLabels();
                    imgInput.Source = BitmapToImageSource(distortedBtm);
                    ImageDistorted = true;
                    distortedBtm.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private Bitmap GetDistortedBitmap(Bitmap scrBitmap, int distPercentage)
        {
            if (distPercentage <= 0 || distPercentage > 100)
            {
                throw new Exception("Invalid percentage value");
            }
            Color newColor = Color.Red;
            int width = scrBitmap.Width;
            int height = scrBitmap.Height;
            Bitmap newBitmap = new Bitmap(scrBitmap);
            Random rand = new Random();

            for (int i = 0; i < width * height * ((double)distPercentage / 100); i++)
            {
                Color randomPixel = newColor;
                int x = -1;
                int y = -1;
                while (randomPixel == newColor)
                {
                    x = rand.Next(width);
                    y = rand.Next(height);
                    randomPixel = scrBitmap.GetPixel(x, y);
                }

                if (x > -1 && y > -1)
                {
                    newBitmap.SetPixel(x, y, newColor);
                }
            }

            scrBitmap.Dispose();
            return newBitmap;
        }
    }
}
