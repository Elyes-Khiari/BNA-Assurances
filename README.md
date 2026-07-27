# 🛡️ BNA Assurances — Plateforme Intelligente d'Assurance Auto

<div align="center">

**Agent IA conversationnel • Devis auto déterministe • Gestion de réclamations • Carte interactive des agences**

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Supabase](https://img.shields.io/badge/Supabase-PostgreSQL-3FCF8E?style=for-the-badge&logo=supabase&logoColor=white)
![Groq](https://img.shields.io/badge/Groq-Llama_3.1-F55036?style=for-the-badge&logo=meta&logoColor=white)
![QuestPDF](https://img.shields.io/badge/QuestPDF-PDF_Generation-blue?style=for-the-badge)
![Leaflet](https://img.shields.io/badge/Leaflet.js-Interactive_Map-199900?style=for-the-badge&logo=leaflet&logoColor=white)

</div>

---

## 📋 Table des Matières

- [Présentation](#-présentation)
- [Fonctionnalités](#-fonctionnalités)
- [Architecture Technique](#-architecture-technique)
- [Stack Technologique](#-stack-technologique)
- [Structure du Projet](#-structure-du-projet)
- [Prérequis](#-prérequis)
- [Installation & Configuration](#-installation--configuration)
- [Lancement](#-lancement)
- [API Endpoints](#-api-endpoints)
- [Base de Données](#-base-de-données)
- [Agent IA — Fonctionnement](#-agent-ia--fonctionnement)
- [Captures d'Écran](#-captures-décran)
- [Auteur](#-auteur)

---

## 🎯 Présentation

**BNA Assurances** est une plateforme web complète développée pour BNA Assurances (filiale de BNA Bank, Tunisie). Elle intègre un **agent conversationnel IA** capable de :

- **Estimer un devis d'assurance auto** en temps réel selon les grilles tarifaires officielles tunisiennes (FTUSA / AMI Assurances).
- **Répondre aux questions** des clients sur les produits, garanties et procédures grâce à une base de connaissances vectorielle (RAG).
- **Guider les réclamations et sinistres** étape par étape avec génération automatique de brouillons.
- **Localiser les agences BNA** sur une carte interactive avec géolocalisation GPS.

L'application s'appuie sur **ASP.NET Core 8 MVC**, **Supabase** (PostgreSQL + pgvector + Storage), et les modèles LLM **Groq Llama 3.1** avec fallback **OpenRouter**.

---

## ✨ Fonctionnalités

### 🤖 Agent IA Conversationnel
| Fonctionnalité | Description |
|---|---|
| **Chat en streaming (SSE)** | Réponses en temps réel via Server-Sent Events sur la page d'accueil et la page Assistant |
| **Questionnaire Devis Guidé** | 5 questions strictes : Puissance fiscale → Usage → Statut conducteur → Modèle/Année → Garanties |
| **Calcul Déterministe** | Moteur C# 100% déterministe — aucun calcul délégué au LLM |
| **Recherche Web de Prix** | Scraping en direct (DuckDuckGo → Tayara.tn, Automobile.tn) pour estimer la valeur vénale |
| **Génération PDF** | Devis PDF professionnel généré avec QuestPDF (logo BNA, tableau des garanties, total) |
| **Envoi par E-mail** | Envoi automatique du PDF en pièce jointe via SendGrid SMTP |
| **Transcription Vocale** | Entrée vocale via Groq Whisper (`whisper-large-v3`) |
| **RAG (Knowledge Base)** | Recherche vectorielle pgvector sur la documentation BNA Assurances |

### 📝 Gestion des Réclamations
| Fonctionnalité | Description |
|---|---|
| **Dépôt guidé par IA** | L'agent collecte les informations étape par étape (type sinistre, date, lieu, description) |
| **Brouillons dynamiques** | Sauvegarde progressive dans Supabase sans perte de données |
| **Vérification de contrat** | Recherche automatique par numéro de permis dans la base clients |
| **Upload de fichiers** | Téléchargement de pièces justificatives (photos, constat amiable) vers Supabase Storage |
| **Espace Suivi Client** | Tableau de bord personnel pour consulter l'état des réclamations |
| **Espace Gestion Assureur** | Panneau d'administration pour les agents BNA (filtres, mise à jour de statut, commentaires) |

### 🗺️ Carte Interactive des Agences
| Fonctionnalité | Description |
|---|---|
| **Leaflet.js + OpenStreetMap** | 30 agences BNA positionnées sur la carte tunisienne |
| **Géolocalisation GPS** | Bouton "Me géolocaliser" avec calcul de distance Haversine |
| **Filtres dynamiques** | Recherche textuelle, filtre par gouvernorat, filtre "Ouvertes maintenant" |
| **Actions rapides** | Appeler (tel:), Itinéraire Google Maps, E-mail (mailto:) |

### 🔐 Authentification & Comptes
| Fonctionnalité | Description |
|---|---|
| **Inscription avec vérification** | Validation du numéro de permis contre la base `ClientRecords` |
| **Connexion sécurisée** | Authentification par cookies ASP.NET Core (durée 30 jours) et mots de passe hachés avec `BCrypt.Net-Next` |
| **Rôles** | `Client` (suivi réclamations) et `Assureur` (gestion back-office) |
| **Mot de passe oublié** | Flux sécurisé par code de vérification à 6 chiffres envoyé par SendGrid SMTP |

---

## 🏗️ Architecture Technique

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLIENT (Navigateur)                      │
│   Index.cshtml (Widget IA) │ Assistant.cshtml (Plein écran)     │
│   NosAgences.cshtml (Carte Leaflet) │ Reclamation/*.cshtml      │
└────────────────────┬────────────────────────────────────────────┘
                     │ HTTP / SSE
┌────────────────────▼────────────────────────────────────────────┐
│                   ASP.NET Core 8 MVC                            │
│                                                                 │
│  ┌─────────────────┐  ┌──────────────────┐  ┌───────────────┐  │
│  │ AgentController  │  │ ReclamationCtrl  │  │ AccountCtrl   │  │
│  │ (SSE + Tools)    │  │ (CRUD + Admin)   │  │ (Auth Cookie) │  │
│  └────────┬────────┘  └────────┬─────────┘  └───────────────┘  │
│           │                    │                                 │
│  ┌────────▼────────────────────▼─────────────────────────────┐  │
│  │                    Services Layer                          │  │
│  │  DevisPricingCalculator │ DevisPdfGenerator                │  │
│  │  ReclamationService     │ ReclamationAgentTools            │  │
│  └────────────────────────────────────────────────────────────┘  │
└──────┬──────────┬──────────┬──────────┬─────────────────────────┘
       │          │          │          │
  ┌────▼───┐ ┌───▼────┐ ┌───▼───┐ ┌───▼──────────┐
  │ Groq   │ │OpenRtr │ │Supab. │ │ SendGrid     │
  │ LLM    │ │Fallback│ │ DB +  │ │ SMTP         │
  │Whisper │ │        │ │Storage│ │              │
  └────────┘ └────────┘ └───────┘ └──────────────┘
```

---

## 🛠️ Stack Technologique

### Backend
| Technologie | Rôle |
|---|---|
| **ASP.NET Core 8 MVC** | Framework web principal (Controllers, Views, Routing) |
| **Entity Framework Core 8** | ORM pour PostgreSQL (Migrations, DbContext) |
| **Npgsql** | Driver PostgreSQL natif pour .NET |
| **QuestPDF** | Génération de documents PDF professionnels |
| **HtmlAgilityPack** | Web scraping HTML pour la recherche de prix véhicules |
| **BCrypt.Net-Next** | Hachage sécurisé des mots de passe utilisateurs |

### Base de Données & Cloud
| Technologie | Rôle |
|---|---|
| **Supabase** | Backend-as-a-Service (PostgreSQL, Auth, Storage, REST API) |
| **pgvector** | Extension PostgreSQL pour la recherche vectorielle (RAG) |
| **Supabase Storage** | Stockage de fichiers (pièces justificatives, photos sinistres) |

### Intelligence Artificielle
| Technologie | Rôle |
|---|---|
| **Groq** (`llama-3.1-8b-instant`) | LLM principal pour le chatbot (inférence ultra-rapide) |
| **OpenRouter** (fallback) | LLM de secours en cas d'indisponibilité Groq |
| **Groq Whisper** (`whisper-large-v3`) | Transcription vocale (Speech-to-Text) |
| **Sentence-Transformers** (Python) | Service d'embeddings pour la vectorisation RAG |

### Frontend
| Technologie | Rôle |
|---|---|
| **Razor Views (.cshtml)** | Moteur de templates ASP.NET Core |
| **TailwindCSS (CDN)** | Framework CSS utilitaire |
| **Leaflet.js** | Carte interactive OpenStreetMap |
| **Lucide Icons** | Bibliothèque d'icônes SVG |

### Services Externes
| Technologie | Rôle |
|---|---|
| **SendGrid SMTP** | Envoi d'e-mails transactionnels (PDF devis en pièce jointe) |
| **DuckDuckGo Search** | Recherche web pour l'estimation des prix véhicules en Tunisie |

---

## 📁 Structure du Projet

```
BNA Assurances/
├── AI/                                # Scripts Python (Embeddings & Indexation RAG)
│   ├── scripts/
│   │   ├── embedding_api.py           # API FastAPI pour les embeddings
│   │   ├── extract.py                 # Extraction de texte depuis les documents
│   │   ├── index.py                   # Indexation locale
│   │   └── index_to_supabase.py       # Ingestion des vecteurs dans Supabase pgvector
│   └── Data/                          # Documents source de la base de connaissances
│
├── Controllers/
│   ├── AccountController.cs           # Authentification (Login, Signup, Logout)
│   ├── AgentController.cs             # Agent IA principal (SSE, Tools, Devis, Email)
│   ├── ChatController.cs              # Endpoint RAG standalone
│   ├── HomeController.cs              # Pages publiques du site vitrine
│   └── ReclamationController.cs       # CRUD réclamations + panneau admin
│
├── Data/
│   └── AppDbContext.cs                # Entity Framework Core DbContext
│
├── Models/
│   ├── ApplicationUser.cs             # Modèle utilisateur (Email, FullName, Role, NumeroPermis)
│   ├── ClientRecord.cs                # Registre client (contrats, véhicules)
│   └── Reclamation.cs                 # Modèle réclamation/sinistre
│
├── Services/
│   ├── DevisPricingCalculator.cs      # Moteur de calcul déterministe (grilles FTUSA)
│   ├── DevisPricingTools.cs           # Schémas d'outils JSON pour l'agent IA
│   ├── DevisPdfGenerator.cs           # Générateur PDF avec QuestPDF
│   ├── ReclamationAgentTools.cs       # Prompts système & outils IA pour réclamations
│   └── ReclamationService.cs          # CRUD Supabase pour les réclamations
│
├── Views/
│   ├── Account/
│   │   ├── Login.cshtml               # Page de connexion
│   │   └── Signup.cshtml              # Page d'inscription
│   ├── Home/
│   │   ├── Index.cshtml               # Page d'accueil avec widget IA intégré
│   │   ├── Assistant.cshtml           # Assistant IA plein écran
│   │   ├── NosAgences.cshtml          # Carte interactive Leaflet.js (30 agences)
│   │   ├── Particuliers.cshtml        # Produits particuliers
│   │   ├── Entreprises.cshtml         # Produits entreprises
│   │   ├── Sinistres.cshtml           # Information sinistres
│   │   ├── Actualites.cshtml          # Actualités BNA
│   │   └── Contact.cshtml             # Page de contact
│   ├── Reclamation/
│   │   ├── Create.cshtml              # Formulaire de dépôt (constat amiable interactif)
│   │   ├── MesReclamations.cshtml     # Suivi client (tableau + modals détaillées)
│   │   └── Gestion.cshtml             # Panneau admin assureur
│   └── Shared/
│       └── _Layout.cshtml             # Layout principal (header, footer, navigation)
│
├── wwwroot/
│   ├── css/site.css                   # Styles globaux
│   ├── js/site.js                     # Scripts globaux (menu mobile, tabs, scroll)
│   └── images/                        # Logo BNA, partenaires
│
├── Program.cs                         # Point d'entrée (DI, Middleware, Auth, Routing)
├── appsettings.json                   # Configuration (Groq, Supabase, SMTP, Embeddings)
└── BNA Assurances.csproj              # Fichier projet .NET 8
```

---

## 📦 Prérequis

| Outil | Version Minimum |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0+ |
| [Git](https://git-scm.com/) | 2.x |
| [Python](https://www.python.org/) | 3.10+ (pour le service d'embeddings) |
| Compte [Supabase](https://supabase.com) | Projet avec extensions `pgvector` activées |
| Clé API [Groq](https://console.groq.com) | Pour le LLM et Whisper |
| Compte [SendGrid](https://sendgrid.com) | Pour l'envoi d'e-mails SMTP |

---

## ⚙️ Installation & Configuration

### 1. Cloner le dépôt

```bash
git clone https://github.com/Elyes-Khiari/BNA-Assurances.git
cd BNA-Assurances
```

### 2. Restaurer les dépendances .NET

```bash
dotnet restore
```

### 3. Configuration & Gestion des Secrets

Par mesure de sécurité (Production Level), les mots de passe et clés API ne doivent **pas** être écrits en clair dans `appsettings.json`. Le projet utilise le Secret Manager de .NET (`dotnet user-secrets`) pour le développement local.

Initialisez d'abord les secrets dans le dossier du projet :

```bash
dotnet user-secrets init
```

Ensuite, ajoutez vos clés privées via ces commandes :

```bash
dotnet user-secrets set "Groq:ApiKey" "<VOTRE_CLE_GROQ>"
dotnet user-secrets set "OpenRouter:ApiKey" "<VOTRE_CLE_OPENROUTER>"
dotnet user-secrets set "Supabase:ServiceKey" "<VOTRE_SERVICE_ROLE_KEY>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<HOST>;Port=5432;Database=postgres;Username=postgres;Password=<PASSWORD>;Ssl Mode=Require;Trust Server Certificate=true;"
dotnet user-secrets set "SmtpSettings:Password" "<VOTRE_CLE_API_SENDGRID>"
```

*Remarque : En production (ex: sur Azure, AWS, Render), vous devrez définir ces clés en tant que **Variables d'Environnement** au lieu d'utiliser `user-secrets`.*

### 4. Configurer le service d'embeddings (Python)

```bash
cd AI
python -m venv .venv
.venv/Scripts/activate       # Windows
pip install fastapi uvicorn sentence-transformers
python scripts/embedding_api.py
```

### 5. Préparer la base de données Supabase

Activez l'extension `pgvector` dans votre projet Supabase et créez les tables nécessaires :
- `messages` — Historique des conversations
- `knowledge_base` — Base de connaissances vectorielle (colonne `embedding vector(384)`)
- `devis_history` — Archivage des devis calculés
- `reclamation_drafts` — Brouillons de réclamations
- `Reclamations` — Réclamations soumises
- `ApplicationUsers` — Comptes utilisateurs
- `ClientRecords` — Registre clients/contrats

Créez la fonction RPC de recherche vectorielle :

```sql
CREATE OR REPLACE FUNCTION match_documents(query_embedding vector(384), match_count int)
RETURNS TABLE (id bigint, content text, similarity float)
LANGUAGE plpgsql AS $$
BEGIN
  RETURN QUERY
  SELECT knowledge_base.id, knowledge_base.content,
         1 - (knowledge_base.embedding <=> query_embedding) AS similarity
  FROM knowledge_base
  ORDER BY knowledge_base.embedding <=> query_embedding
  LIMIT match_count;
END;
$$;
```

---

## 🚀 Lancement

```bash
dotnet run
```

L'application sera accessible sur `https://localhost:5001` (ou `http://localhost:5000`).

---

## 🔌 API Endpoints

### Agent IA (`/api/agent`)

| Méthode | Endpoint | Description |
|---|---|---|
| `POST` | `/api/agent/message` | Envoie un message à l'agent IA (réponse en SSE) |
| `GET` | `/api/agent/history/{sessionId}` | Récupère l'historique d'une conversation |
| `POST` | `/api/agent/speech` | Transcrit un fichier audio (Whisper) et le traite |
| `POST` | `/api/agent/upload` | Upload une pièce jointe pour une réclamation |
| `GET` | `/api/agent/devis/download/{id}` | Télécharge le PDF d'un devis |
| `POST` | `/api/agent/devis/send-email` | Envoie un devis PDF par e-mail |
| `POST` | `/api/agent/devis/search-price` | Recherche le prix d'un véhicule sur le web tunisien |

### Chat RAG (`/api/chat`)

| Méthode | Endpoint | Description |
|---|---|---|
| `POST` | `/api/chat/stream` | Chat RAG avec recherche vectorielle (SSE) |
| `GET` | `/api/chat/history` | Historique du chat |

---

## 🗄️ Base de Données

### Tables Supabase (PostgreSQL)

| Table | Description |
|---|---|
| `ApplicationUsers` | Comptes utilisateurs (email, mot de passe hashé, rôle, numéro permis) |
| `ClientRecords` | Registre des clients assurés (contrats, véhicules, statut) |
| `Reclamations` | Réclamations soumises (type sinistre, statut, commentaires) |
| `reclamation_drafts` | Brouillons de réclamations en cours de rédaction |
| `messages` | Historique des messages du chatbot (par `conversation_id`) |
| `knowledge_base` | Documents vectorisés pour la recherche RAG (pgvector) |
| `devis_history` | Archivage des devis calculés avec détails des garanties |

---

## 🤖 Agent IA — Fonctionnement

### Flux du Devis Auto (5 étapes)

```
Client: "Je veux un devis"
    │
    ├── Q1: Puissance fiscale (CV) ?
    ├── Q2: Usage (privé / professionnel) ?
    ├── Q3: Statut (nouveau conducteur / 2ème véhicule / voiture de fonction) ?
    ├── Q4: Modèle et année du véhicule ?
    └── Q5: Garanties souhaitées (Tout risques, Vol, Incendie...) ?
            │
            ▼
    ┌─────────────────────────────────────┐
    │  1. Recherche Web du prix véhicule  │ (DuckDuckGo → Tayara.tn)
    │  2. Calcul déterministe C#          │ (DevisPricingCalculator)
    │  3. Affichage du devis              │ (Total en DT)
    │  4. Proposition envoi PDF par email  │ (SendGrid SMTP)
    └─────────────────────────────────────┘
```

### Outils disponibles pour l'agent

| Outil | Déclencheur |
|---|---|
| `estimate_devis` | Calcul de prime annuelle avec grilles tarifaires |
| `search_car_price` | Recherche du prix du véhicule sur le marché tunisien |
| `send_devis_email` | Envoi du PDF par e-mail |
| `search_knowledge_base` | Recherche RAG dans la documentation BNA |
| `lookup_client_contracts` | Vérification de contrat par numéro de permis |
| `update_reclamation_draft` | Mise à jour du brouillon de réclamation |
| `submit_reclamation` | Soumission définitive de la réclamation |

### Barème Tarifaire (RC par puissance fiscale)

| Puissance Fiscale | Prime de Base RC (Classe 3 = 100%) |
|---|---|
| 2 CV | 95,000 DT |
| 3 — 4 CV | 110,000 DT |
| 5 — 6 CV | 145,000 DT |
| 7 — 10 CV | 181,000 DT |
| 11 — 14 CV | 210,000 DT |
| 15+ CV | 255,000 DT |

---

## 📸 Captures d'Écran

> Les captures d'écran de l'application sont disponibles dans le dossier `/screenshots` ou dans les artifacts du projet.

---

## 👤 Auteur

**Elyes Khiari**

- GitHub : [@Elyes-Khiari](https://github.com/Elyes-Khiari)
- Projet réalisé dans le cadre d'un stage chez **BNA Assurances** (Tunisie)

---

## 📄 Licence

Ce projet est développé dans un cadre académique / stage professionnel pour BNA Assurances.  
Tous droits réservés © 2025 BNA Assurances.
