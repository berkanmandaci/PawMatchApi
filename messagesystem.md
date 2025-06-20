# PawMatch Chat Sistemi: API ve SignalR Akışı

Aşağıda, PawMatch projesinde chat/messaging sisteminin hem REST API hem de SignalR ile nasıl birlikte çalıştığı şematik olarak gösterilmiştir.

---

```mermaid
sequenceDiagram
    participant FE as "Frontend (Flutter/Web)"
    participant API as "Backend API (REST)"
    participant HUB as "SignalR ChatHub"
    participant DB as "Database"

    Note over FE: Kullanıcı uygulamada chat ekranında

    FE->>API: GET /api/v1/messages/{matchId}\n(Mesaj geçmişini al)
    API->>DB: Mesajları getir (matchId)
    DB-->>API: Mesaj listesi (MessageDto[])
    API-->>FE: JSON mesaj listesi

    FE->>HUB: SignalR ile bağlan (connect)
    FE->>HUB: SendMessage(matchId, content)
    HUB->>DB: Yeni mesajı kaydet
    DB-->>HUB: Kayıt başarılı (Message)
    HUB-->>FE: ReceiveMessage (yeni mesajı anlık ilet)
    HUB-->>API: (Opsiyonel) Bildirim/Log/İstatistik
    Note over FE: FE, ReceiveMessage ile anlık mesajı gösterir

    Note over FE: Tipler ve DTO'lar\nHem API hem SignalR\nda MessageDto, MatchDto\ngibi ortak yapılar kullanılır
```

---

## Açıklama
- Kullanıcı chat ekranında ilk açılışta API'dan mesaj geçmişini çeker.
- SignalR ile bağlanır, yeni mesaj gönderir.
- SignalR üzerinden gelen mesajlar anlık olarak ekranda gösterilir.
- Hem API hem SignalR tarafında aynı DTO'lar (ör. MessageDto) kullanılır, tip tutarlılığı sağlanır.

---

**Not:**
- Bu yapı, modern chat uygulamalarında yaygın olarak kullanılır.
- API geçmiş ve offline mesajlar için, SignalR ise anlık iletişim için kullanılır.
- Tip karışıklığı olmaması için backend'de tüm id alanları int/int? olarak tanımlanmıştır.
