# crossword-api

Kelime Çengel'in backend'i: İngilizce ↔ Türkçe çengel bulmaca üreten ve oyunu yöneten .NET 10 Web API.

Frontend: [crossword-web](https://github.com/eyyazilimci/crossword-web)

## Mimari

Clean Architecture + CQRS:

```
src/
  Crossword.Domain          Entity'ler, bulmaca üretici algoritma, puan/seviye/rozet kuralları (bağımlılık yok)
  Crossword.Application     Komut/sorgu + handler'lar (Mediator), FluentValidation, pipeline behavior'ları
  Crossword.Infrastructure  EF Core + PostgreSQL, kelime seed'i, başlangıçta kütüphane üretimi
  Crossword.Api             Minimal API endpoint'leri, CORS, ProblemDetails, OpenAPI/Scalar
tests/
  Crossword.Domain.Tests    Üretici algoritma, kapsam ve kural testleri (xUnit)
```

- **CQRS:** [Mediator](https://github.com/martinothamar/Mediator) (MIT, source generator). MediatR'ın yeni sürümleri ticari lisanslı olduğu için bu seçildi; kullanımı aynı.
- **Pipeline:** `LoggingBehavior` → `ValidationBehavior` → handler.
- **Dependency Injection:** Her katmanın kendi `AddApplication()` / `AddInfrastructure()` metodu var.

## Bulmaca algoritması

`Crossword.Domain/Crosswords`

- **Bulmaca tipi:** Serbest yerleşimli çengel bulmaca. Kelimeler ortak harflerde kesişir, kullanılmayan hücreler kapalı kalır.
- **Yerleştirme kuralları:** Bir kelimenin önü ve arkası boş kalır, yeni harfler paralel komşuya değmez. Böylece ızgarada istenmeyen kelime oluşmaz.
- **Seçim:** Her bulmaca için birkaç rastgele yerleşim denenir, en iyi skoru alan seçilir. Skor; kelime sayısına, kesişme sayısına ve doluluğa bakar.
- **Boyut:** Çalışma alanı 19–23 hücre arasında seçilir, sonuç kullanılan alana kırpılır. Ortalama 21×21 ve 55–60 kelime çıkar.
- **`LibraryPlanner`:** Bulmaca serisini üretir. Her bulmaca henüz kullanılmamış bir kelimeye sabitlenir, böylece **her kelime en az bir kez** kullanılır. Ardından her zorluk seviyesi için en az `MinPuzzlesPerDifficulty` bulmaca olacak şekilde tamamlar.
- **Uygulama başlarken:** Mod başına zorluk başına 30 bulmaca üretilir; toplamda 2 mod × 3 seviye × 30 = 180 bulmaca. Kütüphane biterse yeni bulmaca anında üretilir.
- **Zayıf kelimeler:** Yanlış yaptığın kelimeler yeni bulmacalarda daha yüksek öncelik alır.

## Kelime listesi

`src/Crossword.Infrastructure/Persistence/Seed/words-*.txt` (`english;turkish;pos`, satır sırası = sıklık sırası)

- **İngilizce:** [New General Service List (NGSL)](https://www.newgeneralservicelist.com/) temel alınarak seçilen 2033 yaygın içerik kelimesi. NGSL, Browne, Culligan & Phillips tarafından CC BY-SA 4.0 lisansıyla yayımlanmıştır.
- **Türkçe çeviriler:** Proje için hazırlandı. Yalnızca tek kelimelik çeviriler alındı.
- **Seviye:** Sıklık sırasından türetilir: ilk 500 A1, sonraki 500 A2, sonraki 500 B1, kalanı B2.
- **Değişiklik:** Dosyayı düzenleyip uygulamayı yeniden başlatman yeterli. Seeder mevcut kelimeleri günceller, yenileri ekler.

## Veritabanı

PostgreSQL. Tabloları elle oluşturmak için `db/schema.sql` dosyasını çalıştır:

```bash
createdb -U postgres crossword
psql -U postgres -d crossword -f db/schema.sql
```

| Tablo | Amaç |
| --- | --- |
| `words` | Kelime çiftleri (`english`, `turkish`, `part_of_speech`, `level`, `frequency_rank`) |
| `puzzles` | Üretilen bulmacalar (`mode`, `difficulty`, `width`, `height`, `seed`, `daily_date`) |
| `puzzle_entries` | Bulmacadaki her kelime (`number`, `orientation`, `row_index`, `column_index`, `answer`, `clue`) |
| `game_sessions` | Oynanışlar (`letters`, `revealed_mask`, `mistake_mask`, süre, puan, XP) |
| `word_masteries` | Kelime başına doğru/yanlış sayısı (mod bazında) |
| `player_profiles` | XP, seri, çözülen bulmaca sayısı (tek satır) |
| `unlocked_achievements` | Açılan rozetler |
| `__EFMigrationsHistory` | EF Core migration kaydı (ileride `dotnet ef database update` için) |

İstersen tabloları uygulamaya da kurdurabilirsin: `appsettings.json` → `Database:AutoMigrate: true`.

## Çalıştırma

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=crossword;Username=postgres;Password=..." --project src/Crossword.Api
dotnet run --project src/Crossword.Api
```

- **API adresi:** http://localhost:5080. Geliştirme modunda API dokümanı /scalar adresinde.
- **İlk açılış:** Kelimeler yüklenir ve kütüphane arka planda üretilir (birkaç saniye sürer).
- **Testler:** `dotnet test`

## Başlıca endpoint'ler

| Method | Yol | Açıklama |
| --- | --- | --- |
| GET | `/api/dashboard` | Ana sayfa özeti |
| GET | `/api/stats` | İstatistikler, zayıf kelimeler, rozetler |
| GET | `/api/puzzles` | Kütüphane + kelime kapsamı |
| POST | `/api/puzzles/next` | Seçilen mod/zorlukta sıradaki bulmaca |
| POST | `/api/puzzles/daily` | Günün bulmacası |
| POST | `/api/puzzles/library` | Kütüphaneye bulmaca ekle |
| POST | `/api/sessions` | Bulmacayı başlat / devam et |
| PUT | `/api/sessions/{id}/progress` | Otomatik kayıt |
| POST | `/api/sessions/{id}/check` | Harf/kelime/bulmaca kontrolü |
| POST | `/api/sessions/{id}/reveal` | İpucu (harf aç) |
| POST | `/api/sessions/{id}/submit` | Bitir; puan, XP, rozet |
