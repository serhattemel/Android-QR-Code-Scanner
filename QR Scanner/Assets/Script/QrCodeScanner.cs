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
    //Sýra numarasýný tanýmlýyor
    public static int sira_no = 1;

    //listeyi tutmamýzý saðlayan deðiþkeni tanýmlýyor
    public Text liste;

    //Kodda kullancaðýmýz gerekli parametleri tanýmlýyoruz.
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

    //_isCamAvaible kameranýn kullanýlabilir olduðunu doðrulayacak deðiþkeni tanýmlýyor.
    //Bu deðiþken bool olduðu için sadece true veya false deðerini alabilir.
    private bool _isCamAvaible;

    //WebCamTexture sýnýfýndan bir deðiþken üretiyoruz
    private WebCamTexture _cameraTexture;

    //Listeyi temizlemizi saðlayan metot
    public void Password_Check()
    {

        if (userID == Password_Field.text)
        {
            Login_Panel.gameObject.SetActive(false);
        }
        else
        {
            Info.text = "HATALI ÞÝFRE GÝRDÝNÝZ";
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
                liste.text = "VERÝTABANI HATASI!";
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

    // Uygulama çalýþtýrýldýðýna otomatik olarak çalýþan baþlangýç metodu
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
        //SetUpCamera metodunu çaðýrýyor.
        SetUpCamera();
#endif
    }

    //Kameramýzýn sürekli olarak çalýþmasýný saðlayan metot
    void Update()
    {
        UpdateCameraRender();
    }

    //Kameramýzýn kullanýlabilir olup olmadýðýný kontrol eden metod
    private void SetUpCamera()
    {

        //Kullanýlabilir cihazlarýn bir listesini döndür
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            _isCamAvaible = false;
            return;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            //WebCamDevice sýnýfýndaki isFrontFacing metodu ile arka kameramýzý asýl kamera olarak ayarlýyoruz
            //eðer ön kamerayý kullanmak istersek aþaðýdaki koþulu true yapabiliriz
            if (devices[i].isFrontFacing == false)
            {
                //WebCamTexture sýnýfýndan belirli özellikleri (ad, en, boy) bulunan bir nesne oluþturuyor ve bunu _cameraTexture'e atýyor.
                //kameranýn ekranda kaplayacaðý alaný belirlirliyor
                _cameraTexture = new WebCamTexture(devices[i].name, (int)_scanZone.rect.width, (int)_scanZone.rect.height);
            }

        }

        //WebCamTexture sýnýfdaki Play metodunu kullanarak Kamerayý baþlatýr.
        _cameraTexture.Play();

        //Kameramýzýn görüntüsü ekrandaki Texture'e yansýtýyor.
        _rawImageBackground.texture = _cameraTexture;

        //_isCamAvaible deðiþkenini true deðerini atýyor.
        _isCamAvaible = true;
    }


    /// <summary>
    /// kameramýzý çalýþtýran kod
    /// </summary>
    private void UpdateCameraRender()
    {
        if (_isCamAvaible == false)
        {
            return;
        }
        //Kameramýzýn görüþ açýsýný belirliyor.
        float ratio = (float)_cameraTexture.width / (float)_cameraTexture.height;
        _aspectRatioFitter.aspectRatio = ratio;

        int orientation = -_cameraTexture.videoRotationAngle;
        _rawImageBackground.rectTransform.localEulerAngles = new Vector3(0, 0, orientation);
    }
    /// <summary>
    /// Buton'a basýldýðýnda çalýþacak metod
    /// </summary>
    public void OnClickScan()
    {
        Scan();

    }

    /// <summary>
    /// Okunan ve listeye eklenecek olan QR'ýn daha öncede listemizde olup olmadýðýný kontrol eden metod
    /// </summary>
    /// <param name="wanted"></param>
    /// <returns></returns>
    bool Search(string wanted)
    {
        //"arr" adýndaki listemizin elemanlarýný alfabetik olarak sýralýyor.
        arr.Sort();
        //listemizin içersinde Binary Search algoritmasý ile liste içerisinde parametre olarak gönderdiðimiz elemaný arýyor
        //ve buluabilirse bu elemanýn index'ini index deðiþkenine atýyor.
        //(C# BinarySearch metodunu içerisinde barýndýðýndan tekrardan kendimiz yazmamýza gerek kalmýyor.)
        int index = arr.BinarySearch(wanted);
        //eðer liste içerisinde aranan elemaný bulabilirse index 0 veya 0'dan büyük olacaktýr.
        //Böylece index<0 ise false deðil ise true deðerini geriye döndürecektir.
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
    //QR kodu tarayýp algýlayan ve algýladýðý kodu listeye ekleyen metot
    private void Scan()
    {
        //Try Catch bloðu sayesinde programda oluþacak hatalarý yakalýyoruz.
        try
        {
            //BarcodeReader sýnýfýndan barcodeReader nesnesini üretiyor.
            IBarcodeReader barcodeReader = new BarcodeReader();

            //yukarýda oluþturduðumuz barcodeReader nesnesini kullanarak kameradaki QR kodu yakalayýp çözümlüyor
            //ve sonucunu result deðiþkenine atýyor.
            var result = barcodeReader.Decode(_cameraTexture.GetPixels32(), _cameraTexture.width, _cameraTexture.height);
            //Listeye eklenecek elemanýn daha önceden listede olup olmadýðýný kontrol edecek metodu çaðýrýyor ve 
            //bunun sonucunu bool deðerinde isfound deðiþkenine atýyor
            isfound = Search(result.Text);
            //Eðer kameramýz QR kodu okuyabilirse ve isfound false ise alttaki if bloðu çalýþýr.
            if (result != null && isfound == false)
            {
                //Ekrana QR kod çýktýsýný yazdýrýr.
                _textOut.text = result.Text;
                //Listemize isimi ekliyor
                arr.Add(result.Text);
                Save_List_To_PP();
                //Listeye sonucu ekler.
                liste.text = string.Concat(liste.text, sira_no, "- ", result.Text, "\n");
                //Sýra numarasýný bir arttýrýr.
                sira_no++;
                last = result.Text;
                Set_List("new_list", liste.text);

            }
            else if (isfound != false)
            {
                _textOut.text = "AYNI QR KOD OKUTULAMAZ, LÜTFEN BAÞKA BÝR QR KOD OKUTUN";
            }
            //Eðer kameramýz QR kodu okuyamaz ise alttaki else bloðu çalýþýr.
            else
            {
                //Ekrana aþaðýdaki çýktýyý yazdýrýr.
                _textOut.text = "QR KOD OKUNAMADI";
            }
        }
        catch
        {
            //ekrana hata mesajýný yazdýrýr
            _textOut.text = "UYGULAMA HATASI!";
        }
    }
}
