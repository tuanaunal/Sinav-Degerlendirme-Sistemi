namespace Sds.OrnekVeri;

/// <summary>Türkiye'de yaygın ad ve soyadlar (OBİS'teki gibi büyük harfle yazılır).</summary>
public static class Isimler
{
    public static readonly string[] Kadin =
    [
        "Ayşe", "Fatma", "Zeynep", "Elif", "Merve", "Büşra", "Esra", "Kübra", "Şeyma", "Rabia",
        "İrem", "Gizem", "Ece", "Damla", "Dilara", "Melike", "Selin", "Sena", "Beyza", "Nur",
        "Hilal", "Tuğba", "Özge", "Gamze", "Yağmur", "Aleyna", "Buse", "Ceren", "Derya", "Ebru",
        "Fadime", "Gülşen", "Hatice", "İlayda", "Kader", "Leyla", "Meryem", "Nisanur", "Öykü", "Pınar",
        "Rümeysa", "Sudenaz", "Şevval", "Tuana", "Ümran", "Vildan", "Yaren", "Zehra", "Cansu", "Duygu",
    ];

    public static readonly string[] Erkek =
    [
        "Mehmet", "Mustafa", "Ahmet", "Ali", "Hüseyin", "Hasan", "İbrahim", "Yusuf", "Emre", "Burak",
        "Murat", "Ömer", "Furkan", "Enes", "Kerem", "Berkay", "Emirhan", "Batuhan", "Oğuzhan", "Çağrı",
        "Doğukan", "Eren", "Fatih", "Gökhan", "Halil", "İsmail", "Kaan", "Mert", "Onur", "Serkan",
        "Sinan", "Şükrü", "Taha", "Tolga", "Uğur", "Volkan", "Yasin", "Zafer", "Abdullah", "Bilal",
        "Cem", "Deniz", "Erdem", "Hakan", "İlker", "Kadir", "Melih", "Necati", "Osman", "Selim",
    ];

    /// <summary>İkinci ad (bazı öğrencilerde: "Mehmet Emin", "Fatma Nur")</summary>
    public static readonly string[] IkinciAdKadin = ["Nur", "Su", "Naz", "Gül", "Nisa"];
    public static readonly string[] IkinciAdErkek = ["Emin", "Can", "Ali", "Berk", "Efe", "Han"];

    public static readonly string[] Soyad =
    [
        "Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Yıldız", "Yıldırım", "Öztürk", "Aydın", "Özdemir",
        "Arslan", "Doğan", "Kılıç", "Aslan", "Çetin", "Kara", "Koç", "Kurt", "Özkan", "Şimşek",
        "Polat", "Özcan", "Korkmaz", "Çakır", "Erdoğan", "Yavuz", "Can", "Acar", "Şen", "Aktaş",
        "Güler", "Yalçın", "Güneş", "Bozkurt", "Bulut", "Keskin", "Ünal", "Turan", "Gül", "Özer",
        "Işık", "Kaplan", "Avcı", "Sarı", "Tekin", "Taş", "Köse", "Yüksel", "Ateş", "Aksoy",
        "Altun", "Bayram", "Bilgin", "Coşkun", "Duman", "Eren", "Gökçe", "Karaca", "Karakaya", "Kocabaş",
        "Konuk", "Okumuş", "Örnek", "Sevim", "Sönmez", "Tunç", "Uçar", "Uysal", "Yaman", "Zengin",
        "Akbulut", "Akgül", "Babaoğlu", "Çiftçi", "Dinçer", "Ekinci", "Güven", "Karabulut", "Mutlu", "Topal",
    ];

    /// <summary>Türkçe kurallarına uygun büyük harf (i → İ, ı → I).</summary>
    public static string Buyuk(string s) => s.ToUpper(new System.Globalization.CultureInfo("tr-TR"));
}
