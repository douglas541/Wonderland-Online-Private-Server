using System;
using System.IO;
using PServer_v2.NetWork;

namespace PServer_v2.DataBase
{
    public class DatabaseInitializer
    {
        private cGlobals globals;
        private AppSettings settings;

        public DatabaseInitializer(cGlobals globals)
        {
            this.globals = globals;
            this.settings = AppSettings.Load();
        }

        public void Initialize()
        {
            bool userDbCreated = InitializeUserDatabase();
            bool gameDbCreated = InitializeGameDatabase();

            if (settings.Environment.IsDevelopment && (userDbCreated || gameDbCreated))
            {
                CreateAdminAccount();
            }
        }

        private bool InitializeUserDatabase()
        {
            string dbPath = settings.Database.UserDatabasePath;
            cDatabase db = new cDatabase(dbPath);
            
            bool created = db.CreateDatabaseIfNotExists();
            
            if (created || !db.TableExists("User"))
            {
                CreateUserTable(db);
                return true;
            }
            
            return created;
        }

        private bool InitializeGameDatabase()
        {
            string dbPath = settings.Database.GameDatabasePath;
            cDatabase db = new cDatabase(dbPath);
            
            bool created = db.CreateDatabaseIfNotExists();
            
            if (created || !db.TableExists("characters"))
            {
                CreateCharactersTable(db);
            }
            
            if (created || !db.TableExists("ImMall"))
            {
                CreateImMallTable(db);
            }
            
            if (created || !db.TableExists("inventory"))
            {
                CreateInventoryTable(db);
            }
            
            return created;
        }

        private void CreateUserTable(cDatabase db)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS User (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                Password TEXT NOT NULL,
                character1ID INTEGER DEFAULT 0,
                character2ID INTEGER DEFAULT 0,
                GMLevel INTEGER DEFAULT 0,
                IM INTEGER DEFAULT 0
            );";
            
            db.ExecuteNonQuery(sql);
        }

        private void CreateCharactersTable(cDatabase db)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS characters (
                characterID INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                nickname TEXT,
                password TEXT,
                map INTEGER DEFAULT 11016,
                x INTEGER DEFAULT 500,
                y INTEGER DEFAULT 1000,
                body INTEGER DEFAULT 0,
                head INTEGER DEFAULT 0,
                colors1 INTEGER DEFAULT 0,
                colors2 INTEGER DEFAULT 0,
                gold INTEGER DEFAULT 0,
                level INTEGER DEFAULT 1,
                exp INTEGER DEFAULT 6,
                curHP INTEGER DEFAULT 100,
                maxHP INTEGER DEFAULT 100,
                curSP INTEGER DEFAULT 50,
                maxSP INTEGER DEFAULT 50,
                element INTEGER DEFAULT 0,
                flags TEXT,
                lastMap TEXT,
                recordSpot TEXT,
                gpsSpot TEXT,
                rebirth INTEGER DEFAULT 0,
                job INTEGER DEFAULT 0,
                stats TEXT,
                sidebar TEXT DEFAULT 'none',
                skills TEXT,
                mail TEXT,
                friends TEXT,
                state INTEGER DEFAULT 1
            );";
            
            db.ExecuteNonQuery(sql);
        }

        private void CreateImMallTable(cDatabase db)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS ImMall (
                ItemID INTEGER PRIMARY KEY,
                Tab INTEGER DEFAULT 0,
                state INTEGER DEFAULT 0,
                Price INTEGER DEFAULT 0,
                Discount INTEGER DEFAULT 0
            );";
            
            db.ExecuteNonQuery(sql);
        }

        private void CreateInventoryTable(cDatabase db)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS inventory (
                characterID INTEGER,
                slot INTEGER,
                itemID INTEGER,
                quantity INTEGER DEFAULT 1,
                PRIMARY KEY (characterID, slot)
            );";
            
            db.ExecuteNonQuery(sql);
        }

        private void CreateAdminAccount()
        {
            try
            {
                cDatabase userDb = new cDatabase(settings.Database.UserDatabasePath);
                cDatabase gameDb = new cDatabase(settings.Database.GameDatabasePath);

                string checkUser = "SELECT COUNT(*) FROM User WHERE Username = '" + settings.Admin.Username.Replace("'", "''") + "';";
                string userCount = userDb.ExecuteScalar(checkUser);

                if (string.IsNullOrEmpty(userCount) || userCount == "0")
                {
                    string insertUser = string.Format(
                        "INSERT INTO User (Username, Password, GMLevel, IM) VALUES ('{0}', '{1}', {2}, 0);",
                        settings.Admin.Username.Replace("'", "''"), 
                        settings.Admin.Password.Replace("'", "''"), 
                        settings.Admin.GMLevel
                    );
                    
                    userDb.ExecuteNonQuery(insertUser);
                    
                    string userId = userDb.ExecuteScalar("SELECT ID FROM User WHERE Username = '" + settings.Admin.Username.Replace("'", "''") + "';");
                    
                    if (!string.IsNullOrEmpty(userId))
                    {
                        uint characterID = GetNextCharacterID(gameDb);
                        
                        string charName = settings.Admin.CharacterName.Replace("'", "''");
                        string insertCharacter = string.Format(
                            "INSERT INTO characters (characterID, name, nickname, password, map, x, y, body, head, " +
                            "colors1, colors2, gold, level, exp, curHP, maxHP, curSP, maxSP, element, flags, " +
                            "lastMap, recordSpot, gpsSpot, rebirth, job, stats, sidebar, skills, mail, friends, state) " +
                            "VALUES ({0}, '{1}', '{1}', '', 11016, 500, 1000, 0, 0, 0, 0, 0, 1, 6, 100, 100, 50, 50, 0, " +
                            "'0 0 0 ', '0 0 0 ', '0 0 0 ', '0 0 0 ', 0, 0, '', 'none', '', '', '', 1);",
                            characterID, charName
                        );
                        
                        gameDb.ExecuteNonQuery(insertCharacter);
                        
                        string updateUser = string.Format(
                            "UPDATE User SET character1ID = {0} WHERE ID = {1};",
                            characterID, userId
                        );
                        
                        userDb.ExecuteNonQuery(updateUser);
                        
                        globals.Log("Conta admin criada: " + settings.Admin.Username);
                    }
                }
                else
                {
                    string userId = userDb.ExecuteScalar("SELECT ID FROM User WHERE Username = '" + settings.Admin.Username.Replace("'", "''") + "';");
                    if (!string.IsNullOrEmpty(userId))
                    {
                        string char1Id = userDb.ExecuteScalar("SELECT character1ID FROM User WHERE ID = " + userId + ";");
                        if (!string.IsNullOrEmpty(char1Id))
                        {
                            string currentMap = gameDb.ExecuteScalar("SELECT map FROM characters WHERE characterID = " + char1Id + ";");
                            if (string.IsNullOrEmpty(currentMap) || currentMap == "0")
                            {
                                gameDb.ExecuteNonQuery("UPDATE characters SET map = 11016, x = 500, y = 1000, state = 1 WHERE characterID = " + char1Id + ";");
                                globals.Log("Personagem admin atualizado com mapa válido.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                globals.Log("Erro ao criar/atualizar conta admin: " + ex.Message);
            }
        }

        private uint GetNextCharacterID(cDatabase db)
        {
            string maxId = db.ExecuteScalar("SELECT MAX(characterID) FROM characters;");
            if (string.IsNullOrEmpty(maxId))
            {
                return 1;
            }
            return uint.Parse(maxId) + 1;
        }
    }
}

