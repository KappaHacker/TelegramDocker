using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBotUser
{
    public class TelegramUser
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }            //айди чата 
        public long migration { get; set; }         //значение миграции в чате с chatId
        public bool setValueCheck { get; set; }     //состояние указывающее что нужно принять значение migration
        public long setterValueId { get; set; } //Message в котором хранится id пользователя запустившего команду set_migration
        public int messageId { get; set; } //Message в котором хранится id закрепленного сообщения после изменения миграции

    }
}
