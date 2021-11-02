using Divibot.Database;
using Divibot.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Divibot.Attack {

    /// <summary>
    /// The types of score-modifying modifiers for attacks.
    /// </summary>
    public enum AttackModifier {
        CRIT,
        INEFF
    }

    /// <summary>
    /// A list of categories for attacks to be grouped into. Used to determine attack chances.
    /// </summary>
    public enum AttackCategory {
        RUDE,
        GROSS,
        JOKING,
        NICE,
        COWARD,
        SAD
    }

    /// <summary>
    /// Represents a type of attack.
    /// </summary>
    public class AttackType {

        /// <summary>
        /// Used to identify the attack type in internal logic.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// A user-friendly name for this attack type for display purposes only.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Constructs a new attack type.
        /// </summary>
        /// <param name="id">Used to identify the attack type in internal logic.</param>
        /// <param name="name">A user-friendly name for this attack type for display purposes only.</param>
        public AttackType(string id, string name) {
            Id = id;
            Name = name;
        }

        public override string ToString() {
            return Id;
        }

    }

    /// <summary>
    /// Represents a class for an attack.
    /// </summary>
    public class AttackClass {

        /// <summary>
        /// Used to identify this class in internal logic.
        /// </summary>
        public string Id { get; protected internal set; }

        /// <summary>
        /// The user-friendly name for this class for display purposes only.
        /// </summary>
        public string Name { get; protected internal set; }

        /// <summary>
        /// The description for this class.
        /// </summary>
        public string Description { get; protected internal set; }

        /// <summary>
        /// The chances this class will have for attacks.
        /// </summary>
        public AttackClassChances Chances { get; protected internal set; }

    }

    /// <summary>
    /// Represents the list of chances (minimums and maximums) for modifiers and attack categories.
    /// </summary>
    public class AttackClassChances {

        /// <summary>
        /// The minimum chances the modifiers can possibly have when being chosen.
        /// </summary>
        public Dictionary<AttackModifier, double> ModifierMins { get; protected internal set; }

        /// <summary>
        /// The maximum chances the modifiers can possibly have when being chosen.
        /// </summary>
        public Dictionary<AttackModifier, double> ModifierMaxs { get; protected internal set; }

        /// <summary>
        /// The minimum chances attacks in the specified categories can have when being chosen.
        /// </summary>
        public Dictionary<AttackCategory, uint> ChanceMins { get; protected internal set; }

        /// <summary>
        /// The maximum chances attacks in the specified categories can have when being chosen.
        /// </summary>
        public Dictionary<AttackCategory, uint> ChanceMaxs { get; protected internal set; }

    }

    public static class AttackConstants {

        /// <summary>
        /// The master list of attacks.
        /// </summary>
        public static Dictionary<AttackCategory, AttackType[]> AttackTypes { get; } = new Dictionary<AttackCategory, AttackType[]>() {
            {
                AttackCategory.RUDE,
                new AttackType[] {
                    new AttackType("SLAP", "Slap"),
                    new AttackType("HIT", "Hit"),
                    new AttackType("KICK", "Kick"),
                    new AttackType("PUNCH", "Punch"),
                    new AttackType("LAUGH_AT", "Laugh At"),
                    new AttackType("MAKE_FACE_AT", "Make Face At"),
                    new AttackType("OFFEND", "Offend"),
                    new AttackType("INVADE_DMS", "Invade DMs"),
                    new AttackType("MOCK", "Mock"),
                    new AttackType("CLAP_AT", "Clap At")
                }
            },
            {
                AttackCategory.GROSS,
                new AttackType[] {
                    new AttackType("PUKE_ON", "Puke On"),
                    new AttackType("FLING_POOP_AT", "Fling Poop At"),
                    new AttackType("LICK", "Lick"),
                    new AttackType("SPIT", "Spit"),
                    new AttackType("BURP_AT", "Burp At")
                }
            },
            {
                AttackCategory.JOKING,
                new AttackType[] {
                    new AttackType("SARCASM", "Sarcasm"),
                    new AttackType("JOKE", "Joke"),
                    new AttackType("DAD_JOKE", "Dad Joke"),
                    new AttackType("UR_MAMA_JOKE", "Ur Mama Joke"),
                    new AttackType("KNEE_SLAPPER", "Knee Slapper")
                }
            },
            {
                AttackCategory.NICE,
                new AttackType[] {
                    new AttackType("LOVE", "Love"),
                    new AttackType("HUG", "Hug"),
                    new AttackType("KISS", "Kiss"),
                    new AttackType("APPRECIATE", "Appreciate"),
                    new AttackType("HOLD_HAND", "Hold Hand"),
                    new AttackType("COMPLIMENT", "Compliment"),
                    new AttackType("PAT_BACK", "Pat Back"),
                    new AttackType("HIGH_FIVE", "High Five"),
                    new AttackType("ENCOURAGE", "Encourage"),
                    new AttackType("WINK_AT", "Wink At")
                }
            },
            {
                AttackCategory.COWARD,
                new AttackType[] {
                    new AttackType("FREEZE", "Freeze"),
                    new AttackType("RUN", "Run"),
                    new AttackType("COWER", "Cower"),
                    new AttackType("HIDE", "Hide"),
                    new AttackType("PANIC", "Panic"),
                    new AttackType("CALL_HELP", "Call for Help")
                }
            },
            {
                AttackCategory.SAD,
                new AttackType[] {
                    new AttackType("TEAR_UP", "Tear Up"),
                    new AttackType("CRY", "Cry"),
                    new AttackType("POUT", "Pout"),
                    new AttackType("COMPLAIN", "Complain"),
                    new AttackType("BE_DEPRESSED", "Be Depressed")
                }
            }
        };

        /// <summary>
        /// A list of predefined default classes.
        /// </summary>
        public static List<AttackClass> Classes { get; } = new List<AttackClass>() {
            new AttackClass() {
                Id = "INTERNAL_TEST",
                Name = "Internal Test",
                Description = "",
                Chances = new AttackClassChances() {
                    ModifierMins = new Dictionary<AttackModifier, double>() {
                        { AttackModifier.CRIT, 0.3 },
                        { AttackModifier.INEFF, 0.7 }
                    },
                    ModifierMaxs = new Dictionary<AttackModifier, double>() {
                        { AttackModifier.CRIT, 0.4 },
                        { AttackModifier.INEFF, 1 }
                    },
                    ChanceMins = new Dictionary<AttackCategory, uint>() {
                        { AttackCategory.RUDE, 5 },
                        { AttackCategory.GROSS, 25 },
                        { AttackCategory.JOKING, 5 },
                        { AttackCategory.NICE, 70 },
                        { AttackCategory.COWARD, 75 },
                        { AttackCategory.SAD, 80 }
                    },
                    ChanceMaxs = new Dictionary<AttackCategory, uint>() {
                        { AttackCategory.RUDE, 10 },
                        { AttackCategory.GROSS, 35 },
                        { AttackCategory.JOKING, 10 },
                        { AttackCategory.NICE, 85 },
                        { AttackCategory.COWARD, 85 },
                        { AttackCategory.SAD, 95 }
                    }
                }
            },
            new AttackClass() {
                Id = "CUSTOM",
                Name = "Custom",
                Description = "A custom class generated specifically for you.",
                Chances = null
            }
        };

    }

    // The primary attack service
    public class AttackService {

        // Dependency Injection
        private Random _random;
        private DivibotDbContext _dbContext;

        // Constructor
        public AttackService(Random random, DivibotDbContext dbContext) {
            _random = random;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Fetches a specific user's class
        /// </summary>
        /// <param name="userId">The user id of the user</param>
        /// <returns>The appropriate AttackClass that this user has, or null if not found</returns>
        public async Task<AttackClass> GetClassForUserAsync(ulong userId) {
            // Get entity
            EntityAttackUser attackUser = await _dbContext.AttackUsers.SingleOrDefaultAsync(c => c.UserId == userId);
            if (attackUser == null) {
                return null;
            }

            // Check for a custom class and apply modifiers/chances
            // Otherwise, find the default
            AttackClass cls;
            if (attackUser.Class == "CUSTOM") {
                cls = AttackConstants.Classes.Single(c => c.Id == "CUSTOM");
                cls = await ApplyCustomClassChances(userId, cls);
            } else {
                cls = AttackConstants.Classes.Single(c => c.Id == attackUser.Class);
            }

            // Return class
            return cls;
        }

        /// <summary>
        /// Applies the custom chances to the given chance replacing the given class's chances. Used for custom classes.
        /// </summary>
        /// <param name="userId">The userId to get custom chances for.</param>
        /// <param name="cls">The class to apply the custom chances to.</param>
        /// <returns>An updated class with the chances applied.</returns>
        public async Task<AttackClass> ApplyCustomClassChances(ulong userId, AttackClass cls) {
            // Fetch chances
            var entityChances = _dbContext.CustomAttackCategoryChances.Where(c => c.UserId == userId);
            var entityModifiers = _dbContext.CustomAttackModifierChances.Where(c => c.UserId == userId);

            // Apply chances
            AttackClassChances chances = new AttackClassChances() {
                ModifierMins = new Dictionary<AttackModifier, double>(),
                ModifierMaxs = new Dictionary<AttackModifier, double>(),
                ChanceMins = new Dictionary<AttackCategory, uint>(),
                ChanceMaxs = new Dictionary<AttackCategory, uint>()
            };
            foreach (EntityCustomAttackCategoryChance entityChance in entityChances) {
                chances.ChanceMins.Add(entityChance.Category, entityChance.ChanceMin);
                chances.ChanceMaxs.Add(entityChance.Category, entityChance.ChanceMax);
            }
            foreach (EntityCustomAttackModifierChance entityModifier in entityModifiers) {
                chances.ModifierMins.Add(entityModifier.Modifier, entityModifier.ChanceMin / 100.0);
                chances.ModifierMaxs.Add(entityModifier.Modifier, entityModifier.ChanceMax / 100.0);
            }
            cls.Chances = chances;

            return cls;
        }

        /// <summary>
        /// Switches a user from their old or nonexistant class to a new one and resets their score.
        /// </summary>
        /// <param name="userId">The user id for the user.</param>
        /// <param name="cls">The class to switch to.</param>
        /// <returns></returns>
        public async Task UpdateUserClassAsync(ulong userId, AttackClass cls) {
            // Fetch for an existing user
            EntityAttackUser attackUser = await _dbContext.AttackUsers.SingleOrDefaultAsync(c => c.UserId == userId);
            if (attackUser != null) {
                // Update class and reset score
                attackUser.Class = cls.Id;
                attackUser.Score = 0;
                _dbContext.AttackUsers.Attach(attackUser);
                _dbContext.Entry(attackUser).Property(c => c.Class).IsModified = true;
                _dbContext.Entry(attackUser).Property(c => c.Score).IsModified = true;
            } else {
                // Create a new user
                attackUser = new EntityAttackUser() {
                    UserId = userId,
                    Class = cls.Id,
                    Score = 0
                };
                _dbContext.AttackUsers.Add(attackUser);
            }

            // Create chances
            foreach (AttackCategory attackCategory in Enum.GetValues(typeof(AttackCategory))) {
                AttackConstants.AttackTypes.TryGetValue(attackCategory, out AttackType[] attackTypes);
                foreach (AttackType attackType in attackTypes) {
                    EntityAttackTypeChance chances = GenerateChancesFor(userId, attackCategory, attackType, cls);
                    _dbContext.AttackTypeChances.Add(chances);
                }
            }

            // Save database
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Generates a new EntityAttackTypeChance based on the category, type, and the user's class with the specified user id.
        /// </summary>
        /// <param name="userId">The user to generate these chances for.</param>
        /// <param name="attackCategory">The category that the attack is from.</param>
        /// <param name="attackType">The attack type to generate chances for.</param>
        /// <param name="attackClass">The class to get the randoms from.</param>
        /// <returns></returns>
        public EntityAttackTypeChance GenerateChancesFor(ulong userId, AttackCategory attackCategory, AttackType attackType, AttackClass attackClass) {
            EntityAttackTypeChance chances = new EntityAttackTypeChance();
            chances.UserId = userId;
            chances.AttackCategory = attackCategory;
            chances.AttackTypeId = attackType.Id;
            chances.Chance = GetChanceFor(attackCategory, attackClass);
            chances.CritChance = Convert.ToUInt32(chances.Chance * GetModifierFor(AttackModifier.CRIT, attackClass));
            chances.IneffChance = Convert.ToUInt32(chances.Chance * GetModifierFor(AttackModifier.INEFF, attackClass));
            return chances;
        }

        /// <summary>
        /// Gets the chances for the specified category based on the given class.
        /// </summary>
        /// <param name="attackCategory">The category to generate chances for.</param>
        /// <param name="attackClass">The class to get the randoms from.</param>
        /// <returns></returns>
        private uint GetChanceFor(AttackCategory attackCategory, AttackClass attackClass) {
            attackClass.Chances.ChanceMins.TryGetValue(attackCategory, out uint minChance);
            attackClass.Chances.ChanceMaxs.TryGetValue(attackCategory, out uint maxChance);
            return Convert.ToUInt32(_random.NextDouble() * (maxChance - minChance) + minChance);
        }

        /// <summary>
        /// Gets the chances for the specified modifier based on the given class.
        /// </summary>
        /// <param name="attackModifier">The modifier to generate chances for.</param>
        /// <param name="attackClass">The class to get the randoms from.</param>
        /// <returns></returns>
        private double GetModifierFor(AttackModifier attackModifier, AttackClass attackClass) {
            attackClass.Chances.ModifierMins.TryGetValue(attackModifier, out double minChance);
            attackClass.Chances.ModifierMaxs.TryGetValue(attackModifier, out double maxChance);
            return _random.NextDouble() * (maxChance - minChance) + minChance;
        }

    }

}
