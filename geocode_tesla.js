const fs = require('fs');
const https = require('https');

const rawText = `İstanbul, Türkiye - North
X5QW+55R Hoca Nasrettin Cd.
Istanbul 34885
Kütahya, Türkiye
No:50 Afyon Kütahya Yolu
Kütahya, Kütahya 43000
Ankara, Türkiye
2 Gazi, Konya Devlet Yolu
Ankara 06560
İstanbul, Türkiye - Gurpinar Road
2J9H+X6F Gurpinar Road
Istanbul 34485
Bolu, Turkiye
157 Hendek Servis Alanı
Hamitli 54300
Isparta, Türkiye
W856+XV 110.sok No.6
Isparta 32100
Edirne, Turkiye
254 Şükrüpaşa, Kıyık Cd
Edirne 220830
Gayrettepe İstanbul, Türkiye
2 Koru sok.
Istanbul, İSTANBUL 34340
Ilgaz Çankırı, Türkiye
Fatih Samsun İstanbul Yolu 4 Yol Mevkii 9/2
Cankiri, Ilgaz 18400
İzmit, Türkiye
QW3W+XRR Ömer Türkçakal Blv.
İzmit, Kocaeli 41040
Çankaya Ankara, Türkiye
No:164 Dumlupınar Blv.
Çankaya/Ankara, Ankara 06510
Keşan, Türkiye
VJFR+PQX Tekirdağ İpsala Yolu
Keşan, Edirne 22800
Kırşehir, Türkiye
No:36 Ankara Kayseri Asfaltı Cd
Mucur/Kırşehir, Kırşehir 40500
Denizli, Türkiye
185 Menderes Blv.
Denizli 20030
Muratpaşa Antalya, Türkiye
Konyaaltı Cd. No:1
Antalya 7010
Balıkesir, Türkiye - Istanbul Bound
İbirler 4699. Cad
Balıkesir 10010
Balıkesir, Türkiye - Izmir Bound
46999 İbirler
Karesi/Balıkesir 10010
Akhisar, Istanbul Bound, Türkiye
45200 Ballıca - Istanbul Bound
Akhisar
Akhisar, Izmir Bound, Türkiye
45200 Ballıca - Izmir Bound
Akhisar
Gemlik, Istanbul Bound, Turkiye
16600 Engürücük
Gemlik
Gemlik, Izmir Bound, Turkiye
16600 Engürücük
Gemlik
İstanbul, Türkiye - South
78 Samandıra Cad.
Istanbul 34885
Eskişehir, Türkiye
8GFHCHV9+XF Ankara Eskişehir Yolu
Eskisehir 26600
Aydin, Türkiye
İzmir Söke Yolu
Aydin, Aydın 09260
Tekirdağ, Türkiye
No:24 Yeşilçay Sk
Tekirdağ, Tekirdağ 59030
Tekkeköy Samsun, Türkiye
Işık Sk. No: 2 Kerimbey Mh.
Samsun 55330
Istanbul Asia, Türkiye
Libadiye Cd.
Istanbul
Afyonkarahisar, Türkiye
5 Mareşal Fevzi Çakmak Blv.
Güvenevler, Afyonkarahisar 03030
Uşak, Türkiye - Merkez
Halil Kaya Gedik Blv. Fevzi Çakmak Mh.
Uşak, Uşak 64300`;

const lines = rawText.split('\n').map(l => l.trim()).filter(l => l.length > 0);
const stations = [];

for (let i = 0; i < lines.length; i += 3) {
    if (i + 2 >= lines.length) {
        if (lines[i] && lines[i+1]) {
           stations.push({ name: lines[i], address: lines[i+1], city: lines[i+1] });
        }
        break;
    }
    stations.push({
        name: lines[i],
        address: lines[i+1],
        city: lines[i+2]
    });
}

function geocode(query) {
    return new Promise((resolve) => {
        const url = `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(query)}&format=json&limit=1`;
        https.get(url, { headers: { 'User-Agent': 'ElektrikliRota/1.0' } }, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                try {
                    const parsed = JSON.parse(data);
                    if (parsed.length > 0) resolve({ lat: parseFloat(parsed[0].lat), lon: parseFloat(parsed[0].lon) });
                    else resolve(null);
                } catch(e) { resolve(null); }
            });
        }).on('error', () => resolve(null));
    });
}

async function run() {
    const results = [];
    let idCounter = 1;
    for (const st of stations) {
        let cleanAddress = st.address.replace(/^[A-Z0-9]{4,}\+[A-Z0-9]+\s*/, '');
        let cleanCity = st.city.replace(/[0-9]{5}/, '').trim();
        let query = `${cleanAddress}, ${cleanCity}`;
        
        let geo = await geocode(query);
        if (!geo) geo = await geocode(cleanCity);
        if (!geo) geo = await geocode(st.name.split(',')[0] + ' Turkey');
        
        let guidId = `a00010${idCounter.toString().padStart(2, '0')}-0000-0000-0000-000000000001`;
        
        results.push({
            id: guidId,
            name: `Tesla Supercharger - ${st.name.split(',')[0]}`,
            brand: "Tesla",
            latitude: geo ? geo.lat : 0,
            longitude: geo ? geo.lon : 0,
            isFastCharge: true,
            acConnectorCount: 0,
            dcConnectorCount: 0,
            hpcConnectorCount: 8,
            address: `${st.address}, ${st.city}`
        });
        idCounter++;
        await new Promise(r => setTimeout(r, 1000));
    }
    fs.writeFileSync('ElektrikliRota.Infrastructure/Data/tesla_stations.json', JSON.stringify(results, null, 2));
    console.log(`Geocoded ${results.length} stations. Zero coords: ${results.filter(r => r.latitude === 0).length}`);
}

run();
