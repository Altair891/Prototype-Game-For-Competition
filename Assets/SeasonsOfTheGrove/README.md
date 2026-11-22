# Seasons of the Grove (Unity)

Port Unity (v1.1: model petani/domba/kastil diperbarui mengikuti gambar referensi) dari game "Penanaman Hutan": sandbox santai bergaya Tiny Glade, 4 musim, gelombang intruder, tembok/kastil, NPC dan toko koin.
Seluruh dunia 3D, karakter, hewan, suara, dan UI dibuat lewat kode saat Play, jadi **tidak perlu menyusun scene atau prefab**.

## Pasang (5 menit)
1. Pasang **Unity 6** (diuji pengguna di 6000.6). `Boot.cs` memakai `FindAnyObjectByType` (Unity 2023.1+).
2. Unity Hub > New project > **3D (Built-In Render Pipeline)** (disarankan; URP juga bisa).
3. Salin folder `Assets/` dari zip ini ke folder `Assets/` proyek (gabungkan/timpa).
4. Buka scene apa saja (mis. SampleScene), tekan **Play**. Game membangun dirinya sendiri lewat `Boot.cs`.

Input: berjalan di Input Manager lama, Input System baru, maupun keduanya ("Both").

## Build (penting)
Skrip memakai `Shader.Find(...)` saat runtime. Agar shader tidak dibuang saat build:
Edit > Project Settings > Graphics > **Always Included Shaders**, tambahkan:
- `Standard` (Built-in) atau `Universal Render Pipeline/Lit` (URP)
- `Skybox/Procedural`
- `Sprites/Default`
Lalu File > Build Settings > Add Open Scenes > Build (Windows/Mac/Linux/WebGL).

## Kontrol
- Kamera bebas: **WASD/panah** geser, **klik kanan + geser** putar dan miring, **roda** zoom ke kursor, **klik tengah + geser** geser, **Q/E** putar.
- **Klik petani** atau **F**: kamera terbang ke petani dan mengikutinya. Klik peta mini untuk pindah kamera.
- Brush **1 Benih, 2 Hujan, 3 Sapu, 4 Pagar**. Klik-tahan intruder 2,5 detik untuk mengusir. Klik warga (setelah 15 pohon dewasa) untuk berdagang.
- **H** mode foto, **Esc** jeda.

## Struktur skrip (`Assets/Scripts`)
| File | Isi |
|---|---|
| `Boot.cs` | Membuat semua komponen otomatis |
| `Sim.cs` | Logika game: tile, pertumbuhan, api, gelombang, intruder, koin, toko |
| `World.cs` | Seluruh tampilan 3D: tanah, pohon, kastil, NPC, petani, hewan, langit, danau |
| `CamRig.cs` | Kamera bebas |
| `Play.cs` | Input pemain (brush, mengusir, klik NPC/petani) |
| `GameUI.cs` | Menu utama gaya Tiny Glade, HUD, toko, jeda, pengaturan (IMGUI) |
| `Sfx.cs` | Suara prosedural |
| `StaticReset.cs`, `WorldGen.cs`, `BakedWorld.cs`, `Editor/WorldBaker.cs` | Reset cache saat Play, pembangun dunia statis, dan alat Bake dunia permanen |
| `Decor.cs` | Ornamen dunia: perbukitan, batu bertekstur, batu besar, formasi batu, lingkaran batu, dataran batu berumput, rumah terbengkalai, reruntuhan, kerajaan besar, bunga/jamur/tunggul, dermaga |
| `Trees.cs` | 10 jenis pohon (bulat, kubus, lonjong, cemara bertingkat, cemara oranye, palem, tiga gumpal, tumpukan, semak, sipres) sebagai mesh gabungan |
| `Birds.cs` | Model burung chickadee dan perilaku terbang berbasis tujuan |
| `Gfx.cs` | Tingkat kualitas grafis (Built-in dan URP) |
| `Models.cs` | Kit mesh prosedural: kastil batu (menara bulat/persegi/spire/oktagon, gerbang lengkung, rumah genteng), karakter chibi low-poly, domba |
| `Util.cs`, `In.cs` | Helper material/primitif dan pembungkus input |

Logo ada di `Assets/Resources/Logo.png` (ganti dengan file Anda sendiri bila perlu).

## Dunia permanen (v1.4): objek tidak hilang setelah Stop
Secara default seluruh dunia dibuat lewat skrip saat Play, jadi objeknya hilang saat Stop. Untuk menjadikannya objek Scene + asset permanen:
1. Hentikan Play, buka scene (mis. SampleScene).
2. Menu **Tools > Seasons of the Grove > Buat Dunia Permanen (Bake)**. Tunggu beberapa detik.
3. Simpan scene (Ctrl+S). Objek `SeasonsWorld_Baked` muncul di Hierarchy; mesh, material, dan tekstur tersimpan di `Assets/SeasonsOfTheGrove_Baked/` beserta prefab `SeasonsWorld_Baked.prefab`.
4. Tekan Play: game otomatis memakai dunia permanen (hutan latar, bukit, batu, rumah/reruntuhan, kerajaan, danau, kastil zona). Bagian dinamis (pohon yang ditanam, NPC, petani, domba, burung, tembok, api) tetap dibuat saat Play karena memang berubah-ubah.
5. Untuk membuang: **Tools > Seasons of the Grove > Hapus Dunia Permanen**.

## Stabilitas (v1.4)
- Cache statis (material/mesh/tekstur) dikosongkan setiap Play (`StaticReset.cs`). Ini memperbaiki `MissingReferenceException ... Material has been destroyed` dan `NullReferenceException` di `World.Dress/Update` yang muncul pada Play kedua ketika Reload Domain dimatikan.
- Tiap langkah pembangunan dunia dibungkus penangkap error; jika satu bagian gagal, error dicatat sekali dan game tidak membanjiri Console.

## Ornamen dunia (v1.3)
Semua ornamen berada di luar area bermain, statis, dan digabung dengan Static Batching: 16 bukit berpohon + 9 bukit raksasa di cakrawala (berkabut), ratusan batu bertekstur berlumut, 3 gugus batu besar, formasi batu bertingkat, lingkaran batu berdiri, 8 dataran/pilar batu berumput, 3 rumah terbengkalai, 3 reruntuhan, kerajaan besar dengan desa di kaki bukit (barat laut), bunga, jamur, tunggul, batang tumbang, semak, alang-alang, dan dermaga di danau. Warna rumput bukit mengikuti musim. Pohon latar memakai mesh berpoligon rendah.

## Performa (v1.2)
- Pohon kini **1 renderer per pohon** (mesh gabungan) dan hanya diperbarui saat perlu: puluhan kali lebih sedikit objek dibanding sebelumnya.
- Langit/kabut/ambient diperbarui 2x per detik (mengubah skybox tiap frame memaksa Unity menghitung ulang refleksi).
- Reflection Probe danau: resolusi 64, dirender bertahap per sisi, tiap 8 detik (bisa dimatikan di Pengaturan).
- Teks HUD dihitung 4x per detik tanpa alokasi string tiap frame.
- Domba 6 renderer (bukan sekitar 38), burung 3 renderer.
- Pengaturan: Kualitas grafis (Rendah/Sedang/Tinggi, mengubah jarak bayangan, cascade, resolusi bayangan, MSAA, render scale lewat salinan asset URP), Refleksi air, Tampilkan FPS, dan Kualitas otomatis (menurunkan kualitas bila FPS rata-rata di bawah 42).

## Kenapa lebih stabil daripada versi web
- Tanah = satu quad bertekstur 30x30 (bukan ratusan mesh); hanya tile yang berubah yang diperbarui.
- Pohon dibuat sekali per sel dan dipakai ulang; material dibagi (batching/instancing); semua collider dibuang.
- Bayangan berkaskade bawaan Unity; refleksi danau memakai Reflection Probe yang diperbarui tiap 1,5 detik.
- Properti statis (kastil, hutan latar) digabung dengan Static Batching.

## Penyetelan jika masih berat
Menu Pengaturan: Bayangan (Mati/Rendah/Tinggi), Anti-aliasing, Peta mini. Di `World.cs`: kurangi jumlah pohon latar (loop `140`), hewan (`8` domba, `6` burung), dan `probe.resolution`.

## Status
Kode ditulis tanpa bisa dijalankan di Unity (tidak ada Unity di lingkungan penulisan). Jika ada error kompilasi, salin pesan dari Console dan kirim agar diperbaiki.


## Dunia permanen (lengkap)
Bake kini juga menyimpan tanah/peta, petani, NPC beserta rumahnya, domba, tembok, dan awan. Yang tetap dibuat saat Play: pohon yang ditanam, pagar, api, intruder, burung, matahari, kamera, dan partikel.
