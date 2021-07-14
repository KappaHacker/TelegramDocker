using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBotUser
{
    public class TelegramUser
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }            //айди чата 
        public long Migration { get; set; }         //значение миграции в чате с chatId
        public bool SetValueCheck { get; set; }     //состояние указывающее что нужно принять значение migration
        public long SetterValueId { get; set; } //Message в котором хранится id пользователя запустившего команду set_migration
        public int MessageId { get; set; } //Message в котором хранится id закрепленного сообщения после изменения миграции

    }
}
