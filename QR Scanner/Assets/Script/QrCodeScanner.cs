using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZXing;

public class QrCodeScanner : MonoBehaviour
{

    public string userID;
    public string sifre = "123";
    public bool isfound;
    public List<String> arr = new List<String>();
    private int SavedListCount;

    public Text Info;
    [SerializeField]
    private Text Password_Field;
    DatabaseReference reference;

    public string last = "   ";
    //S�ra numaras�n� tan�ml�yor
    public static int sira_no = 1;

    //listeyi tutmam�z� sa�layan de�i�keni tan�ml�yor
    public Text liste;

    //Kodda kullanca��m�z gerekli parametleri tan�ml�yoruz.
    [SerializeField]
    private RawImage _rawImageBackground;


    [SerializeField]
    private AspectRatioFitter _aspectRatioFitter;

    [SerializeField]
    private TextMeshProUGUI _textOut;

    [SerializeField]
    private RectTransform _scanZone;

    [SerializeField]
    private RectTransform Login_Panel;

    //_isCamAvaible kameran�n kullan�labilir oldu�unu do�rulayacak de�i�keni tan�ml�yor.
    //Bu de�i�ken bool oldu�u i�in sadece true veya false de�erini alabilir.
    private bool _isCamAvaible;

    //WebCamTexture s�n�f�ndan bir de�i�ken �retiyoruz
    private WebCamTexture _cameraTexture;

    //Listeyi temizlemizi sa�layan metot
    public void Password_Check()
    {

        if (userID == Password_Field.text)
        {
            Login_Panel.gameObject.SetActive(false);
        }
        else
        {
            Info.text = "HATALI ��FRE G�RD�N�Z";
            Login_Panel.gameObject.SetActive(true);
        }


    }
    public void Delete()
    {
        sira_no = 1;
        Set_List("old_list", liste.text);
        Set_List("new_list", liste.text = "");
        arr.Clear();

    }
    public void Get_Old()
    {
        reference.Child("old_list").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                liste.text = "HATA!";
                // Handle the error...
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                liste.text = snapshot.Value.ToString();
                // Do something with snapshot...
            }

        });
    }
    public void Get_New()
    {
        reference.Child("new_list").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                liste.text = "VER�TABANI HATASI!";
                // Handle the error...
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                liste.text = snapshot.Value.ToString();
                // Do something with snapshot...
            }

        });
    }
    void Set_List(string list, string list_text)
    {
        reference.Child(list).SetValueAsync(list_text);
    }

    // Uygulama �al��t�r�ld���na otomatik olarak �al��an ba�lang�� metodu
    void Start()
    {
        reference = FirebaseDatabase.DefaultInstance.RootReference;

        reference.Child("password").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            userID = snapshot.Value.ToString();

        });
        Load_List_On_PP();


#if UNITY_ANDROID
        //SetUpCamera metodunu �a��r�yor.
        SetUpCamera();
#endif
    }

    //Kameram�z�n s�rekli olarak �al��mas�n� sa�layan metot
    void Update()
    {
        UpdateCameraRender();
    }

    //Kameram�z�n kullan�labilir olup olmad���n� kontrol eden metod
    private void SetUpCamera()
    {

        //Kullan�labilir cihazlar�n bir listesini d�nd�r
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            _isCamAvaible = false;
            return;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            //WebCamDevice s�n�f�ndaki isFrontFacing metodu ile arka kameram�z� as�l kamera olarak ayarl�yoruz
            //e�er �n kameray� kullanmak istersek a�a��daki ko�ulu true yapabiliriz
            if (devices[i].isFrontFacing == false)
            {
                //WebCamTexture s�n�f�ndan belirli �zellikleri (ad, en, boy) bulunan bir nesne olu�turuyor ve bunu _cameraTexture'e at�yor.
                //kameran�n ekranda kaplayaca�� alan� belirlirliyor
                _cameraTexture = new WebCamTexture(devices[i].name, (int)_scanZone.rect.width, (int)_scanZone.rect.height);
            }

        }

        //WebCamTexture s�n�fdaki Play metodunu kullanarak Kameray� ba�lat�r.
        _cameraTexture.Play();

        //Kameram�z�n g�r�nt�s� ekrandaki Texture'e yans�t�yor.
        _rawImageBackground.texture = _cameraTexture;

        //_isCamAvaible de�i�kenini true de�erini at�yor.
        _isCamAvaible = true;
    }


    /// <summary>
    /// kameram�z� �al��t�ran kod
    /// </summary>
    private void UpdateCameraRender()
    {
        if (_isCamAvaible == false)
        {
            return;
        }
        //Kameram�z�n g�r�� a��s�n� belirliyor.
        float ratio = (float)_cameraTexture.width / (float)_cameraTexture.height;
        _aspectRatioFitter.aspectRatio = ratio;

        int orientation = -_cameraTexture.videoRotationAngle;
        _rawImageBackground.rectTransform.localEulerAngles = new Vector3(0, 0, orientation);
    }
    /// <summary>
    /// Buton'a bas�ld���nda �al��acak metod
    /// </summary>
    public void OnClickScan()
    {
        Scan();

    }

    /// <summary>
    /// Okunan ve listeye eklenecek olan QR'�n daha �ncede listemizde olup olmad���n� kontrol eden metod
    /// </summary>
    /// <param name="wanted"></param>
    /// <returns></returns>
    bool Search(string wanted)
    {
        //"arr" ad�ndaki listemizin elemanlar�n� alfabetik olarak s�ral�yor.
        arr.Sort();
        //listemizin i�ersinde Binary Search algoritmas� ile liste i�erisinde parametre olarak g�nderdi�imiz eleman� ar�yor
        //ve buluabilirse bu eleman�n index'ini index de�i�kenine at�yor.
        //(C# BinarySearch metodunu i�erisinde bar�nd���ndan tekrardan kendimiz yazmam�za gerek kalm�yor.)
        int index = arr.BinarySearch(wanted);
        //e�er liste i�erisinde aranan eleman� bulabilirse index 0 veya 0'dan b�y�k olacakt�r.
        //B�ylece index<0 ise false de�il ise true de�erini geriye d�nd�recektir.
        if (index < 0)
            return false;
        else
            return true;
    }
    public void Save_List_To_PP()
    {
        for (int i = 0; i < arr.Count; i++)
            PlayerPrefs.SetString("Players" + i, arr[i]);

        PlayerPrefs.SetInt("Count",arr.Count);
    }

    public void Load_List_On_PP()
    {
        arr.Clear();
        SavedListCount=PlayerPrefs.GetInt("Count");

        for(int i=0;i<SavedListCount;i++)
        {
            string player = PlayerPrefs.GetString("Players" + i);
            arr.Add(player);
        }
    }
    //QR kodu taray�p alg�layan ve alg�lad��� kodu listeye ekleyen metot
    private void Scan()
    {
        //Try Catch blo�u sayesinde programda olu�acak hatalar� yakal�yoruz.
        try
        {
            //BarcodeReader s�n�f�ndan barcodeReader nesnesini �retiyor.
            IBarcodeReader barcodeReader = new BarcodeReader();

            //yukar�da olu�turdu�umuz barcodeReader nesnesini kullanarak kameradaki QR kodu yakalay�p ��z�ml�yor
            //ve sonucunu result de�i�kenine at�yor.
            var result = barcodeReader.Decode(_cameraTexture.GetPixels32(), _cameraTexture.width, _cameraTexture.height);
            //Listeye eklenecek eleman�n daha �nceden listede olup olmad���n� kontrol edecek metodu �a��r�yor ve 
            //bunun sonucunu bool de�erinde isfound de�i�kenine at�yor
            isfound = Search(result.Text);
            //E�er kameram�z QR kodu okuyabilirse ve isfound false ise alttaki if blo�u �al���r.
            if (result != null && isfound == false)
            {
                //Ekrana QR kod ��kt�s�n� yazd�r�r.
                _textOut.text = result.Text;
                //Listemize isimi ekliyor
                arr.Add(result.Text);
                Save_List_To_PP();
                //Listeye sonucu ekler.
                liste.text = string.Concat(liste.text, sira_no, "- ", result.Text, "\n");
                //S�ra numaras�n� bir artt�r�r.
                sira_no++;
                last = result.Text;
                Set_List("new_list", liste.text);

            }
            else if (isfound != false)
            {
                _textOut.text = "AYNI QR KOD OKUTULAMAZ, L�TFEN BA�KA B�R QR KOD OKUTUN";
            }
            //E�er kameram�z QR kodu okuyamaz ise alttaki else blo�u �al���r.
            else
            {
                //Ekrana a�a��daki ��kt�y� yazd�r�r.
                _textOut.text = "QR KOD OKUNAMADI";
            }
        }
        catch
        {
            //ekrana hata mesaj�n� yazd�r�r
            _textOut.text = "UYGULAMA HATASI!";
        }
    }
}
