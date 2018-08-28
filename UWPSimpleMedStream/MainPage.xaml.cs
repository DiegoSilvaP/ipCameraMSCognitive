using System;
using System.IO;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.WindowsAzure.MobileServices;
using Windows.UI;

// La plantilla de elemento Página en blanco está documentada en https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0xc0a

namespace UWPSimpleMedStream
{
    /// <summary>
    /// Página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //static readonly string OxfordApiKey = "8e90ce2eb206439b92f4e0cfcc6c93d5"; // Trial
        static readonly string OxfordApiKey = "491eb9cb37e64596b44461fd51abf270"; // Enigma Key
        FaceServiceClient FaceService1 = new FaceServiceClient(OxfordApiKey);
        FaceServiceClient faceService = new FaceServiceClient(OxfordApiKey, "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");
        public MainPage()
        {
            this.InitializeComponent();
            
        }

        private async Task Streaming()
        {
            var URL = "rtsp://"+txtUser.Text + ":"+txtPass.Password + "@"+txtIP.Text+ "/onvif-media/media.amp";
            try
            {
                if (MPEAdaptive.Source != null)
                {

                    MPEAdaptive.MediaPlayer.stop();
                    MPEAdaptive.Source = null;
                    MPEAdaptive.Source = URL;
                    //MPEAdaptive.Source = "@C://Users//diego//OneDrive//Imágenes//18422863_646158178922062_4886626057200462666_o.jpg";
                    MPEAdaptive.MediaPlayer.play();
                }
                else
                {

                    MPEAdaptive.Source = URL;
                    //MPEAdaptive.Source = "@C://Users//diego//OneDrive//Imágenes//18422863_646158178922062_4886626057200462666_o.jpg";
                    MPEAdaptive.MediaPlayer.play();
                }
            }
            catch (Exception ex)
            {
                MessageDialog msj = new MessageDialog(ex.Message, "Error");
                await msj.ShowAsync();
            }

        }

        private async void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            await Streaming();
        }

        private async void btnRecognize_Click(object sender, RoutedEventArgs e)
        {
            await OnSubmitToOxfordAsync();

        }

        private async Task OnSubmitToOxfordAsync()
        {
            try
            {


                progress1.Visibility = Visibility.Visible;
                txtAge.Text = "";
                txtConf.Text = "";
                txtId.Text = "";
                txtGlasses.Text = "";
                txtDescription.Text = "";
                txtName.Text = "";
                txtAlert.Text = "";
                var requiredFaceAttributes = new FaceAttributeType[] {
                FaceAttributeType.Age,
                FaceAttributeType.Gender,
                FaceAttributeType.Smile,
                FaceAttributeType.FacialHair,
                FaceAttributeType.HeadPose,
                FaceAttributeType.Glasses
            };

                string path = Path.GetTempPath() + "file1.jpg";
                MPEAdaptive.MediaPlayer.takeSnapshot(0, path, 1280, 720);

                using (Stream s = File.OpenRead(@path))
                {

                    Face[] faces = await FaceService1.DetectAsync(s, returnFaceLandmarks: true, returnFaceAttributes: requiredFaceAttributes);

                if (faces.Length > 0)
                {
                    foreach (var face in faces)
                    {

                        var idGuid = Guid.Parse(face.FaceId.ToString());
                        //Here we get the range of simility respect our data on the Azure Cloud.
                        var facescomp = await FaceService1.FindSimilarAsync(idGuid, "21122012", 1);
                        var confidence = Double.Parse((facescomp[0].Confidence.ToString(CultureInfo.InvariantCulture)))*100;
                        var lentes = face.FaceAttributes.Glasses.ToString();
                        Debug.WriteLine("Conf: " + confidence.ToString());

                        txtConf.Text = confidence.ToString()+" %";
                        txtId.Text = "ID: "+idGuid.ToString();
                        txtGlasses.Text = lentes;

                        Query(facescomp[0].PersistedFaceId.ToString());
                        
                    }
                }
                    else
                    {
                        txtId.Text = "No match";
                    }
                }
                progress1.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageDialog msj = new MessageDialog(ex.Message, "Error");
            }
        }

        IMobileServiceTable<UsersUPT> todoTable = App.MobileService.GetTable<UsersUPT>();

        private async void Query(string searchId)
        {
            txtAlert.Text = "Alert!!!";
            try
            {
                List<UsersUPT> lista = new List<UsersUPT>();
                //lista = await todoTable.ToListAsync();
                lista = await todoTable.Where(userTableObj => userTableObj.PID == searchId).ToListAsync();
                Debug.WriteLine(lista[0].nombre);
                Debug.WriteLine(lista[0].PID.ToString());

                txtAge.Text = lista[0].edad;
                txtDescription.Text = lista[0].descripcion;
                txtName.Text = lista[0].nombre;
            }
            catch (Exception ex)
            {
                Debug.Write("Error: " + ex);
            }
        }
    }
}

