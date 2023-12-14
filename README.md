# Demo Testcontainers PostgreSQL

Adapté pour faciliter la gestion de dragons plutôt que des clients parce que dragons c'est bien plus cool. (Référence au bas de la page.)

## Introduction

Testcontainers est une bibliothèque de tests qui fournit des API simples et légères pour amorcer des tests d'intégration avec des services réels enveloppés dans des conteneurs Docker. Avec Testcontainers, on pourra écrire des tests qui s'adressent au même type de services que ceux utilisés en production, sans simulations ni services en mémoire.

# Creer la solution avec une source et un projet de test

## Configuration de l'environnement de développement

Assurez-vous d'avoir .NET 7 et un d'avoir installé un [environnement Docker compatible](https://www.testcontainers.org/supported_docker_environment/).

Par exemple:

```shell
$ dotnet --list-sdks
7.0.104 [C:\Program Files\dotnet\sdk]
$ docker version
...
Server: Docker Desktop 4.12.0 (85629)
 Engine:
  Version:          20.10.17
  API version:      1.41 (minimum version 1.12)
  Go version:       go1.17.11
...
```

## Méthode rapide

Cloner le dépôt

```sh
git clone https://github.com/DavidFV2296190/Demo_Testcontainers_PostgreSQL.git
cd Demo_Testcontainers_PostgreSQL
dotnet test
```

Tous les tests devraient être complétés avec succès.

## Méthode manuelle

Créer la source .Net et le projet de test soit dans le terminal ou bien votre environnement de développement de choix.

```sh
$ dotnet new sln -o Demo_Testcontainers_PostgreSQL
$ cd Demo_Testcontainers_PostgreSQL
$ dotnet new classlib -o ServiceDragon
$ dotnet sln add ./ServiceDragon/ServiceDragon.csproj
$ dotnet new xunit -o ServiceDragon.Tests
$ dotnet sln add ./ServiceDragon.Tests/ServiceDragon.Tests.csproj
$ dotnet add ./ServiceDragon.Tests/ServiceDragon.Tests.csproj reference ./ServiceDragon/ServiceDragon.csproj
```

Ajouter la package **Npgsql** au projet

```sh
dotnet add ./ServiceDragon/ServiceDagon.csproj package Npgsql
```

### Implémenter la logique d'affaires

Il faut tout d'abor créer la classe ```ServiceDragon``` pour pouvoir gérer des ```Dragons```

Créer d'abord la classe ```Dragon```

```csharp
namespace Dragons;

public readonly record struct Dragon(long Id, string Name, int Age);
```

Créer ensuite la classe ```DbConnectionProvider``` pour fournir un acces au données

```csharp
using System.Data.Common;
using Npgsql;

namespace Dragons;

public sealed class DbConnectionProvider
{
    private readonly string m_connectionString;

    public DbConnectionProvider(string connectionString)
    {
       m_connectionString = connectionString;
    }

    public DbConnection GetConnection()
    {
        return new NpgsqlConnection(m_connectionString);
    }
}
```

Créer la classe ```ServiceDragons``` et y insérer le code suivant:

```csharp
namespace Dragons;

public sealed class ServiceDragon
{
    private readonly DbConnectionProvider m_dbConnectionProvider;

    public ServiceDragon(DbConnectionProvider dbConnectionProvider)
    {
        m_dbConnectionProvider = dbConnectionProvider;
        CreateDragonsTable();
    }

    public IEnumerable<Dragon> GetDragons()
    {
        IList<Dragon> dragons = new List<Dragon>();

        using var connection = m_dbConnectionProvider.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, age FROM dragons";
        command.Connection?.Open();

        using var dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            var id = dataReader.GetInt64(0);
            var name = dataReader.GetString(1);
            var age = dataReader.GetInt32(2);
            dragons.Add(new Dragon(id, name, age));
        }

        return dragons;
    }

    public void Create(Dragon dragon)
    {
        using var connection = m_dbConnectionProvider.GetConnection();
        using var command = connection.CreateCommand();

        var id = command.CreateParameter();
        id.ParameterName = "@id";
        id.Value = dragon.Id;

        var name = command.CreateParameter();
        name.ParameterName = "@name";
        name.Value = dragon.Name;

        var age = command.CreateParameter();
        age.ParameterName = "@age";
        age.Value = dragon.Age;

        command.CommandText = "INSERT INTO dragons (id, name, age) VALUES(@id, @name, @age)";
        command.Parameters.Add(id);
        command.Parameters.Add(name);
        command.Parameters.Add(age);
        command.Connection?.Open();
        command.ExecuteNonQuery();
    }

    private void CreateDragonsTable()
    {
        using var connection = m_dbConnectionProvider.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS dragons (id BIGINT NOT NULL, name VARCHAR NOT NULL, age INT NOT NULL, PRIMARY KEY (id))";
        command.Connection?.Open();
        command.ExecuteNonQuery();
    }
}
```

Quelques petits détails sur la classe ```ServiceDragons```:
- ```m_dbConnectionProvider.GetConnection()``` récupère la connection en utilisant ADO.NET
- La méthode ```CreateDragonsTable()``` créé une table ```dargons``` si elle n'existe pas déjà.
- La méthode ```GetDragons()``` récupère toutes les lignes de la table ```dragons```, insère les données dans les objets de type ```Dragon``` et renvoie une liste d'objets de type ```Dragon```
- La méthode ```Create(Dragon)``` insère un nouveau dragon dans la base de données.

### Ajouter les dépendances de Testcontainers

Avant de développer des tests en utilisant Testcontainers, on doit ajouter les modules au projet. Ici on ajoutera le module Postgreql:

```sh
dotnet add ./ServiceDragon.Tests/ServiceDragon.Tests.csproj package Testcontainers.PostgreSql
```

### Écrire un test qui utilise Testcontainers

Dans le projet de test, créer la classe ```ServiceDragonsTests``` et y insérer le code suivant:

```csharp
using Testcontainers.PostgreSql;

namespace Dragons.Tests;

public sealed class ServiceDragonTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer m_postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();

    public Task InitializeAsync()
    {
        return m_postgres.StartAsync();
    }

    public Task DisposeAsync()
    {
        return m_postgres.DisposeAsync().AsTask();
    }

    [Fact]
    public void DevraisRetournerDeuxDragons()
    {
        // Arranger
        var serviceDragon = new ServiceDragon(new DbConnectionProvider(m_postgres.GetConnectionString()));

        // Agir
        serviceDragon.Create(new Dragon(1, "Shenron", 850));
        serviceDragon.Create(new Dragon(2, "Smaug", 150));
        var dragons = serviceDragon.GetDragons();

        // Assert
        Assert.Equal(2, dragons.Count());
    }
}
```

Quelques petits détails sur la classe ```ServiceDragonsTests```:

- Création d'un ```PostgreSqlContainer``` en utilisant l'image Docker ```postgres:15-alpine``` au builder.
- Le conteneur Postgres est démarré en utilisant l'infterface ```IAsyncLifetime``` de xUnit.net qui exécute ```InitializeAsync``` lorsque la classe est créée.
- ```DevraitRetournerDeuxDragons()``` initialise ```ServiceDragon```, insèere deux dragons dans la base de données, récupère tous les dragons existants et vérifie que le nombre de dragons est égal à 2.
- Finalement, le conteneur Postgres est détruit dans ```DisposeAsync``` qui est éxécuté après l'exécution des méthodes de test.


Tester avec la commande
```sh
dotnet test
```
Tous les tests devraient être complétés avec succès.

## Conclusion

Nous avons exploré comment utiliser la bibliothèque Testcontainers pour .NET afin de tester une application .NET de gestion de dragons utilisant une base de données Postgres.

Nous avons vu comment écrire un test d'intégration en utilisant Testcontainers est très similaire à l'écriture d'un test unitaire que vous pouvez exécuter depuis votre environnement de développement.

En plus de Postgres, Testcontainers fournit des modules dédiés pour de nombreuses bases de données SQL couramment utilisées, des bases de données NoSQL, des files d'attente de messages(RabbitMQ), etc. Vous pouvez utiliser Testcontainers pour des dépendances conteneurisées pour vos tests !

Vous pouvez trouver plus d'informations sur Testcontainers à [https://www.testcontainers.com/](https://www.testcontainers.com/).

## Références

[Getting started with Testcontainers for .NET](https://testcontainers.com/guides/getting-started-with-testcontainers-for-dotnet/)