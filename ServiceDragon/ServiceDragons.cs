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