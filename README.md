# Documentation du Projet : Gestionnaire de Bases de Données

Cette courte documentation explique le fonctionnement, les fonctionnalités et les technologies utilisées pour le développement de ce gestionnaire de bases de données unifié.

## 1. Fonctionnement de l'application

L'application agit comme un client universel permettant à l'utilisateur de se connecter à plusieurs SGBD sans changer d'outil.
À l'ouverture, l'utilisateur gère ses **profils de connexion** dans la colonne de gauche (ajout, modification, suppression). Chaque profil contient les informations d'identification (Hôte, Port, Utilisateur, Mot de passe) pour un SGBD ciblé.

Une fois la connexion établie, l'interface bascule vers un **explorateur de schéma** et un **éditeur de requêtes**. 
L'explorateur permet de naviguer visuellement (sous forme d'arborescence) dans les bases de données et les tables du serveur. L'éditeur de requêtes, quant à lui, permet de formuler et d'exécuter des instructions (SQL ou NoSQL) et d'afficher les résultats dans un tableau dynamique.

## 2. Fonctionnalités réalisées

* **Gestion multi-SGBD :** Connexion transparente à PostgreSQL, MySQL et MongoDB depuis une interface unique.
* **Sauvegarde des profils :** Mémorisation locale et sécurisée des informations de connexion pour éviter les ressaisies.
* **Opérations CRUD simplifiées :** Possibilité de créer ou supprimer une base de données et de créer ou supprimer une table directement via des boutons dans l'explorateur.
* **Explorateur visuel hiérarchique :** Visualisation en temps réel des bases de données disponibles et de leurs tables (ou collections).
* **Console d'exécution :** Exécution de requêtes personnalisées avec affichage des erreurs directement dans la grille de résultats, évitant ainsi les pop-ups bloquantes.

## 3. Technologies utilisées

* **Langage & Framework :** C# sur le framework .NET 8.0.
* **Interface Graphique :** WPF (Windows Presentation Foundation) couplé au modèle d'architecture MVVM (Model-View-ViewModel) grâce au package `CommunityToolkit.Mvvm`.
* **Connecteurs SGBD :**
  * `Npgsql` : Pilote ADO.NET pour la communication avec **PostgreSQL**.
  * `MySql.Data` : Pilote ADO.NET pour la communication avec **MySQL**.
  * `MongoDB.Driver` : Pilote officiel pour la communication avec **MongoDB**.
* **Stockage local :** JSON (via `System.Text.Json`) pour sauvegarder le fichier de configuration des profils.
