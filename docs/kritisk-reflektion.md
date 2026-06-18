# Kritisk reflektion – TeacherAid

## Vad fungerade

**RAG-flödet fungerade bättre än förväntat.** Att kombinera pgvector direkt i PostgreSQL med Ollama lokalt visade sig vara ett praktiskt val — ett enda Docker-stack hanterar databas, vektorsökning och AI-inferens utan externa beroenden. Sökhastigheten var tillräcklig för kursmaterial i den storleksordning som är aktuell.

**AI sparade tid på rätt ställen.** Boilerplate-kod för ASP.NET Core — controllers, DTO:er, migrationer — genererades snabbt och korrekt. Det frigjorde tid till de delar som faktiskt kräver domänförståelse: hur feedbackflödet ska se ut, vilka avgränsningar som är rimliga för en lärare, hur RAG-kontexten ska struktureras i prompten.

**n8n som orkestreringsverktyg fungerade.** Webhook-triggad automation med ett visuellt gränssnitt var lättare att felsöka än ett alternativ i ren kod hade varit. Det gick att se exakt vilket steg i workflowen som misslyckades, vilket snabbade upp iterationen.

**Valet att köra AI lokalt höll.** Ollama med llama3 och nomic-embed-text gav tillräcklig kvalitet för prototypen, och beslutet innebär att inga studentdata skickas till externa tjänster — något som är viktigt i en utbildningskontext.

---

## Vad fungerade inte

**AI-genererad kod behövde mer granskning än förväntat på affärslogiken.** Grundstrukturen var korrekt, men detaljer som felhantering, edge cases och meningsfulla HTTP-statuskoder saknades konsekvent i första utkast. Exempelvis kastade API:et ohanterade undantag om Ollama var under uppstart istället för att returnera 503. Det är inte ett misstag som är svårt att hitta — men det är ett misstag som AI inte flaggade för.

**Chunking-strategin var naiv.** `TextChunker.Chunk()` splittar enbart på radbrytningar, vilket innebär att ett långt stycke utan radbrytning kan bli en chunk som överstiger maxstorleken. RAG-kvaliteten är direkt beroende av chunk-kvaliteten, och det borde ha prioriterats högre tidigt i projektet.

**Tre chunks per fråga oavsett relevans är ett trubbigt val.** Det fungerar för korta, precisa frågor, men ger sämre resultat om relevant information är utspridd. En tröskel på cosine similarity hade gett mer konsistenta svar.

**Gränssnittets UX kom i kläm.** Feedback visas inte direkt kopplat till inlämningen, och läraren kan inte redigera AI-feedbacken inline. Dessa brister är inte tekniskt svåra att lösa, men de påverkar hur naturligt verktyget känns att använda. Det är lätt att prioritera ner UX när man bygger ensam mot en deadline.

---

## Vad jag skulle göra annorlunda

**Börja med chunking.** RAG-flödets kvalitet är ett tak för hela systemet. Att investera mer tid i meningsbaserad chunking och relevansfiltrering tidigt hade förbättrat outputen mer än ytterligare features.

**Explicit felhanteringskontrakt från start.** Istället för att lägga till felhantering i efterhand: definiera tidigt vad varje endpoint ska returnera vid varje felfall och skriv det som krav. AI är bra på att implementera ett definierat kontrakt, men dålig på att självständigt identifiera vilka felfall som spelar roll.

**Göra feedbackredigeringen inline.** Den enskilt viktigaste saknade funktionen ur lärarens perspektiv. Utan den är verktyget ett utkastverktyg snarare än ett arbetsflöde.

**Cachelagra embeddings.** Ollama-anropet per fråga är onödigt om samma textchunk frågats om tidigare. En enkel cache hade minskat latensen märkbart.

**FAQ-cache av studentfrågor är en fälla.** En idé som övervägdes var att spara AI-svar på vanliga studentfrågor som en FAQ och låta RAG hämta därifrån för att minska antalet Ollama-anrop. Problemet är att ett felaktigt AI-svar som sparas i FAQ:n förstärks — RAG hämtar det som kontext och reproducerar felet i framtida svar. Samma sak sker om kursdokumentet uppdateras men FAQ:n inte ogiltigförklaras. Fördelen i tokenbesparing väger inte upp risken att ett fel self-reinforces utan en explicit mekanism för validering och invalidering av cachad kontext.

---

## När AI är rätt verktyg — och när det inte är det

Det som tydligast framkommer av det här projektet är att AI-assisterad utveckling förskjuter var tänkandet behöver ske, inte hur mycket tänkande som krävs.

**AI är rätt verktyg** när uppgiften är väldefinierad och outputen verifierbar: generera en controller som följer ett givet mönster, skriv ett migrationsscript, föreslå en datamodell utifrån ett beskrivet problem. Här är AI snabb och tillräckligt tillförlitlig, och fel är lätta att fånga i granskning.

**AI är fel verktyg** — eller i alla fall otillräckligt — när uppgiften kräver domänkunskap om vad som faktiskt spelar roll. Vilken chunking-strategi som ger bra RAG-resultat för pedagogiskt material är inte något AI kan avgöra utan att man specificerar det i detalj. Vilka felfall som är kritiska i en lärares arbetsflöde kräver förståelse för hur läraren faktiskt jobbar. AI kan implementera svaret på dessa frågor, men kan inte ställa dem.

Den praktiska slutsatsen är att AI-verktyg i systemutveckling fungerar bäst som en snabb implementeringspartner, inte som en arkitekt. Arkitektbesluten — vad systemet ska göra, vad som är tillräckligt bra, var edge cases spelar roll — behöver fortfarande komma från utvecklaren. Den som försöker delegera också de besluten till AI riskerar att bygga rätt system på fel antaganden.
