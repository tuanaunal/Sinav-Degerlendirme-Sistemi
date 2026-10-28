# Yol Haritası

Her aşama bir öncekinin üzerine eklenir. Aşama tamamlanınca commit atılır ve `asama-XX` etiketi konur.

| # | Aşama | Durum |
|---|---|---|
| 0 | Proje iskeleti ve Selçuk kurumsal teması | ✅ |
| 1 | Veritabanı tasarımı (EF Core, PostgreSQL, başlangıç verisi) — [şema](VERITABANI.md) | ✅ |
| 2 | Giriş ve kimlik doğrulama (JWT, roller, ilk girişte şifre değişikliği) | ✅ |
| 3 | Yönetim paneli (şifre sıfırlama, kullanıcı işlemleri, notlandırma yönetmeliği, optik form ayarları) | ✅ |
| 4 | Ders panosu ve ders detayı | ✅ |
| 5 | Gerçekçi örnek veri dosyaları (120 öğrenci) — [ayrıntılar](../ornek-veriler/README.md) | ✅ |
| 6 | Dosya okuma motoru (optik `.txt`, OBİS `.xlsx`) | ✅ |
| 7 | Çoktan seçmeli sihirbaz: dosya yükleme, cevap anahtarı | ✅ |
| 8 | Çoktan seçmeli sihirbaz: veri eşleştirme, not düzenleme | ✅ |
| 9 | Çoktan seçmeli sihirbaz: kitapçık soru eşleştirme, öğrenme çıktıları | ✅ |
| 10 | Açık uçlu sınav akışı, Excel'e aktar, sınavı sil, düzenleme modu | ✅ |
| 11 | Sınav Başarı Analiz Raporu | ✅ |
| 12 | Resmi Excel formu (Analiz, Liste, İÖK Kağıt) | ✅ |
| 13 | Dönem değerlendirme ve harf notları | ✅ |
| 14 | Dağıtım: Docker, Nginx, GitHub Actions | ⏳ |
| — | *Final teslimi:* AI destekli analiz ve değerlendirme | 🔜 |

## Notlandırma esasları (Selçuk Üniversitesi)

Kaynak: Ön Lisans ve Lisans Eğitim-Öğretim ve Sınav Yönetmeliği, Madde 13 ve Ölçme ve Değerlendirme Esasları Yönergesi.

| Puan | Harf | Katsayı | Durum |
|---|---|---|---|
| 88–100 | AA | 4,00 | Başarılı |
| 80–87 | BA | 3,50 | Başarılı |
| 73–79 | BB | 3,00 | Başarılı |
| 66–72 | CB | 2,50 | Başarılı |
| 60–65 | CC | 2,00 | Başarılı |
| 55–59 | DC | 1,50 | Şartlı geçer |
| 50–54 | DD | 1,00 | Şartlı geçer |
| 0–49 | FF | 0,00 | Başarısız |

- Yarıyıl içi %40, final veya bütünleme %60. Bütünleme finalin yerine geçer.
- Final veya bütünlemeden 100 üzerinden en az 30 alınmalıdır; altında kalan FF alır.
- Test sınavlarında en az 25 soru sorulur.
