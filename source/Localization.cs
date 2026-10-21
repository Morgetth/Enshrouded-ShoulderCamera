using System;
using System.Collections.Generic;

namespace ShoulderCamera {
    internal static class Localizer {
        internal static readonly string[] Codes={"en","fr","de","es","pt-BR"};
        internal static readonly string[] Names={"English","Français","Deutsch","Español","Português (Brasil)"};
        internal static string Language="en";
        static readonly Dictionary<string,string[]> Texts=new Dictionary<string,string[]>(StringComparer.Ordinal){
            {"ENSHROUDED   /   RÉGLAGES DE CAMÉRA",new string[]{"ENSHROUDED   /   CAMERA SETTINGS","ENSHROUDED   /   RÉGLAGES DE CAMÉRA","ENSHROUDED   /   KAMERAEINSTELLUNGEN","ENSHROUDED   /   AJUSTES DE CÁMARA","ENSHROUDED   /   AJUSTES DA CÂMERA"}},
            {"CONNECTER AU JEU",new string[]{"CONNECT TO GAME","CONNECTER AU JEU","MIT SPIEL VERBINDEN","CONECTAR AL JUEGO","CONECTAR AO JOGO"}},
            {"RESTAURER LE JEU",new string[]{"RESTORE CAMERA","RESTAURER LE JEU","KAMERA ZURÜCKSETZEN","RESTAURAR CÁMARA","RESTAURAR CÂMERA"}},
            {"CHANGER D'ÉPAULE",new string[]{"SWITCH SHOULDER","CHANGER D'ÉPAULE","SCHULTER WECHSELN","CAMBIAR DE HOMBRO","TROCAR DE OMBRO"}},
            {"PROFILS",new string[]{"PROFILES","PROFILS","PROFILE","PERFILES","PERFIS"}},
            {"EXPLORATION",new string[]{"EXPLORATION","EXPLORATION","ERKUNDUNG","EXPLORACIÓN","EXPLORAÇÃO"}},
            {"ARC / BAGUETTE",new string[]{"BOW / WAND","ARC / BAGUETTE","BOGEN /\nZAUBERSTAB","ARCO / VARITA","ARCO / VARINHA"}},
            {"BÂTON",new string[]{"STAFF","BÂTON","STAB","BASTÓN","CAJADO"}},
            {"ARC ET BAGUETTE",new string[]{"BOW AND WAND","ARC ET BAGUETTE","BOGEN UND ZAUBERSTAB","ARCO Y VARITA","ARCO E VARINHA"}},
            {"VISÉE AU BÂTON",new string[]{"STAFF AIMING","VISÉE AU BÂTON","STABZIELEN","APUNTADO CON BASTÓN","MIRA COM CAJADO"}},
            {"Activer ce profil",new string[]{"Enable this profile","Activer ce profil","Profil aktivieren","Activar este perfil","Ativar este perfil"}},
            {"ACTIVÉ",new string[]{"ENABLED","ACTIVÉ","AKTIV","ACTIVADO","ATIVADO"}},
            {"DÉSACTIVÉ",new string[]{"DISABLED","DÉSACTIVÉ","INAKTIV","DESACTIVADO","DESATIVADO"}},
            {"Profil activé",new string[]{"Profile enabled","Profil activé","Profil aktiviert","Perfil activado","Perfil ativado"}},
            {"Profil désactivé",new string[]{"Profile disabled","Profil désactivé","Profil deaktiviert","Perfil desactivado","Perfil desativado"}},
            {"Configuration",new string[]{"Configuration","Configuration","Konfiguration","Configuración","Configuração"}},
            {"Langue",new string[]{"Language","Langue","Sprache","Idioma","Idioma"}},
            {"DÉCALAGE HORIZONTAL",new string[]{"HORIZONTAL OFFSET","DÉCALAGE HORIZONTAL","HORIZONTALER VERSATZ","DESPLAZAMIENTO HORIZONTAL","DESLOCAMENTO HORIZONTAL"}},
            {"HAUTEUR SUPPLÉMENTAIRE",new string[]{"ADDED HEIGHT","HAUTEUR SUPPLÉMENTAIRE","ZUSÄTZLICHE HÖHE","ALTURA ADICIONAL","ALTURA ADICIONAL"}},
            {"Décalage horizontal",new string[]{"Horizontal offset","Décalage horizontal","Horizontaler Versatz","Desplazamiento horizontal","Deslocamento horizontal"}},
            {"Hauteur supplémentaire",new string[]{"Added height","Hauteur supplémentaire","Zusätzliche Höhe","Altura adicional","Altura adicional"}},
            {"Valeur du décalage horizontal",new string[]{"Horizontal offset value","Valeur du décalage horizontal","Wert des horizontalen Versatzes","Valor del desplazamiento horizontal","Valor do deslocamento horizontal"}},
            {"Valeur de la hauteur supplémentaire",new string[]{"Added height value","Valeur de la hauteur supplémentaire","Wert der zusätzlichen Höhe","Valor de la altura adicional","Valor da altura adicional"}},
            {"Le jeu passe automatiquement d'un profil à l'autre selon votre action.",new string[]{"The game selects a profile automatically based on your action.","Le jeu passe automatiquement d'un profil à l'autre selon votre action.","Das Spiel wählt je nach Aktion automatisch ein Profil.","El juego elige el perfil automáticamente según tu acción.","O jogo seleciona o perfil automaticamente conforme sua ação."}},
            {"Cliquez sur une valeur pour la saisir · Entrée pour valider",new string[]{"Click a value to type · Enter to confirm","Cliquez sur une valeur pour la saisir · Entrée pour valider","Wert anklicken und eingeben · Eingabe bestätigt","Haz clic en un valor para escribir · Intro para confirmar","Clique no valor para digitar · Enter para confirmar"}},
            {"Marche, course, attente et accroupissement.",new string[]{"Walking, running, idle and crouching.","Marche, course, attente et accroupissement.","Gehen, Laufen, Stehen und Schleichen.","Caminar, correr, reposo y agacharse.","Caminhada, corrida, repouso e agachamento."}},
            {"Vues de visée partagées par l'arc et la baguette.",new string[]{"Shared aiming views for bows and wands.","Vues de visée partagées par l'arc et la baguette.","Gemeinsame Zielansichten für Bogen und Zauberstab.","Vistas de apuntado compartidas por arco y varita.","Vistas de mira compartilhadas por arco e varinha."}},
            {"Visée et canalisation du bâton, debout ou accroupi.",new string[]{"Staff aiming and channeling, standing or crouched.","Visée et canalisation du bâton, debout ou accroupi.","Zielen und Kanalisieren mit dem Stab, stehend oder geduckt.","Apuntado y canalización con bastón, de pie o agachado.","Mira e canalização com cajado, em pé ou agachado."}},
            {"Profil personnalisé · les autres profils gardent leurs propres valeurs.",new string[]{"Custom profile · other profiles keep their own settings.","Profil personnalisé · les autres profils gardent leurs propres valeurs.","Eigenes Profil · andere Profile behalten ihre Einstellungen.","Perfil personalizado · los demás conservan sus ajustes.","Perfil personalizado · os outros mantêm seus ajustes."}},
            {"Profil natif · les réglages d'origine du jeu sont conservés.",new string[]{"Native profile · the game's original settings are preserved.","Profil natif · les réglages d'origine du jeu sont conservés.","Originalprofil · die Einstellungen des Spiels bleiben erhalten.","Perfil original · se conservan los ajustes del juego.","Perfil original · os ajustes do jogo são preservados."}},
            {"Non connecté · aucun réglage appliqué",new string[]{"Not connected · no settings applied","Non connecté · aucun réglage appliqué","Nicht verbunden · keine Einstellungen angewendet","Sin conexión · ningún ajuste aplicado","Desconectado · nenhum ajuste aplicado"}},
            {"Sauvegarde automatique des réglages",new string[]{"Settings save automatically","Sauvegarde automatique des réglages","Einstellungen werden automatisch gespeichert","Los ajustes se guardan automáticamente","Os ajustes são salvos automaticamente"}},
            {"Réglages enregistrés automatiquement",new string[]{"Settings saved automatically","Réglages enregistrés automatiquement","Einstellungen automatisch gespeichert","Ajustes guardados automáticamente","Ajustes salvos automaticamente"}},
            {"Échec de la sauvegarde automatique",new string[]{"Automatic save failed","Échec de la sauvegarde automatique","Automatisches Speichern fehlgeschlagen","Error al guardar automáticamente","Falha ao salvar automaticamente"}},
            {"Enregistrement des réglages…",new string[]{"Saving settings…","Enregistrement des réglages…","Einstellungen werden gespeichert…","Guardando ajustes…","Salvando ajustes…"}},
            {"Recherche des réglages de caméra…",new string[]{"Finding camera settings…","Recherche des réglages de caméra…","Kameraeinstellungen werden gesucht…","Buscando ajustes de cámara…","Buscando ajustes da câmera…"}},
            {"Jeu fermé · reconnectez après son prochain lancement.",new string[]{"Game closed · reconnect after restarting it.","Jeu fermé · reconnectez après son prochain lancement.","Spiel geschlossen · nach dem Neustart erneut verbinden.","Juego cerrado · vuelve a conectar tras iniciarlo.","Jogo fechado · reconecte após iniciá-lo."}},
            {"Préférences illisibles · réglages par défaut utilisés.",new string[]{"Cannot read preferences · using defaults.","Préférences illisibles · réglages par défaut utilisés.","Einstellungen nicht lesbar · Standardwerte verwendet.","No se pueden leer los ajustes · se usan los predeterminados.","Não foi possível ler os ajustes · usando os padrões."}},
            {"Jeu connecté · les profils cochés sont appliqués automatiquement.",new string[]{"Connected · enabled profiles apply automatically.","Jeu connecté · les profils cochés sont appliqués automatiquement.","Verbunden · aktivierte Profile werden automatisch angewendet.","Conectado · los perfiles activados se aplican automáticamente.","Conectado · os perfis ativados são aplicados automaticamente."}},
            {"Jeu connecté · tous les profils utilisent la caméra d'origine.",new string[]{"Connected · all profiles use the original camera.","Jeu connecté · tous les profils utilisent la caméra d'origine.","Verbunden · alle Profile verwenden die Originalkamera.","Conectado · todos los perfiles usan la cámara original.","Conectado · todos os perfis usam a câmera original."}},
            {"Caméra d'origine restaurée · reconnectez pour réappliquer vos profils.",new string[]{"Original camera restored · reconnect to reapply your profiles.","Caméra d'origine restaurée · reconnectez pour réappliquer vos profils.","Originalkamera wiederhergestellt · zum Anwenden der Profile erneut verbinden.","Cámara original restaurada · vuelve a conectar para aplicar tus perfiles.","Câmera original restaurada · reconecte para aplicar seus perfis."}},
            {"Distance et FOV : dans le jeu.\nFermer le panneau restaure la caméra.",new string[]{"Distance and FOV: in the game.\nClosing this panel restores the camera.","Distance et FOV : dans le jeu.\nFermer le panneau restaure la caméra.","Abstand und Sichtfeld: im Spiel.\nSchließen stellt die Originalkamera wieder her.","Distancia y FOV: en el juego.\nCerrar el panel restaura la cámara.","Distância e FOV: no jogo.\nFechar o painel restaura a câmera."}},
            {"Restauration incomplète",new string[]{"Restore incomplete","Restauration incomplète","Wiederherstellung unvollständig","Restauración incompleta","Restauração incompleta"}},
            {"Fermez le jeu pour supprimer les réglages temporaires, puis fermez le panneau.",new string[]{"Close the game to discard temporary changes, then close this panel.","Fermez le jeu pour supprimer les réglages temporaires, puis fermez le panneau.","Schließen Sie das Spiel, um temporäre Änderungen zu verwerfen, dann dieses Fenster.","Cierra el juego para descartar los cambios temporales y después cierra el panel.","Feche o jogo para descartar as alterações temporárias e depois feche o painel."}},
            {"Lecture camera refusee",new string[]{"Camera read denied","Lecture camera refusee","Kameradaten konnten nicht gelesen werden","Lectura de cámara denegada","Leitura da câmera negada"}},
            {"Plusieurs tableaux camera : connexion refusee.",new string[]{"Multiple camera tables: connection refused.","Plusieurs tableaux camera : connexion refusee.","Mehrere Kameratabellen: Verbindung abgelehnt.","Varias tablas de cámara: conexión rechazada.","Várias tabelas de câmera: conexão recusada."}},
            {"Configuration camera introuvable dans la recherche limitee.",new string[]{"Camera settings not found in the bounded search.","Configuration camera introuvable dans la recherche limitee.","Kameraeinstellungen im begrenzten Suchbereich nicht gefunden.","No se encontraron ajustes de cámara en la búsqueda limitada.","Ajustes da câmera não encontrados na busca limitada."}},
            {"Tableau camera incoherent.",new string[]{"Invalid camera table.","Tableau camera incoherent.","Ungültige Kameratabelle.","Tabla de cámara no válida.","Tabela de câmera inválida."}},
            {"Ouvrez un seul client Enshrouded et chargez une partie.",new string[]{"Open one Enshrouded client and load a world.","Ouvrez un seul client Enshrouded et chargez une partie.","Öffnen Sie einen Enshrouded-Client und laden Sie eine Welt.","Abre un solo cliente de Enshrouded y carga un mundo.","Abra apenas um cliente do Enshrouded e carregue um mundo."}},
            {"Version du jeu non prise en charge. Aucune ecriture effectuee.",new string[]{"Unsupported game version. Nothing was written.","Version du jeu non prise en charge. Aucune ecriture effectuee.","Nicht unterstützte Spielversion. Keine Daten geschrieben.","Versión del juego no compatible. No se ha escrito nada.","Versão do jogo incompatível. Nenhum dado foi gravado."}},
            {"Connexion au jeu refusee",new string[]{"Game connection denied","Connexion au jeu refusee","Verbindung zum Spiel verweigert","Conexión al juego denegada","Conexão ao jogo negada"}},
            {"Le jeu est ferme ou la connexion est perdue.",new string[]{"The game is closed or the connection was lost.","Le jeu est ferme ou la connexion est perdue.","Das Spiel ist geschlossen oder die Verbindung ging verloren.","El juego está cerrado o se ha perdido la conexión.","O jogo está fechado ou a conexão foi perdida."}},
            {"Configuration camera remplacee : reconnectez le panneau.",new string[]{"Camera settings replaced: reconnect the panel.","Configuration camera remplacee : reconnectez le panneau.","Kameraeinstellungen ersetzt: erneut verbinden.","Ajustes de cámara reemplazados: vuelve a conectar el panel.","Ajustes da câmera substituídos: reconecte o painel."}},
            {"Alignement camera invalide.",new string[]{"Invalid camera alignment.","Alignement camera invalide.","Ungültige Kameraausrichtung.","Alineación de cámara no válida.","Alinhamento da câmera inválido."}},
            {"Protection camera inattendue : ecriture refusee.",new string[]{"Unexpected camera protection: write refused.","Protection camera inattendue : ecriture refusee.","Unerwarteter Kameraschutz: Schreiben verweigert.","Protección de cámara inesperada: escritura rechazada.","Proteção inesperada da câmera: gravação recusada."}},
            {"Acces temporaire au reglage refuse",new string[]{"Temporary settings access denied","Acces temporaire au reglage refuse","Temporärer Zugriff auf Einstellungen verweigert","Acceso temporal al ajuste denegado","Acesso temporário ao ajuste negado"}},
            {"Ecriture du reglage echouee",new string[]{"Writing the setting failed","Ecriture du reglage echouee","Schreiben der Einstellung fehlgeschlagen","Error al escribir el ajuste","Falha ao gravar o ajuste"}},
            {"Verification du reglage echouee.",new string[]{"Settings verification failed.","Verification du reglage echouee.","Prüfung der Einstellung fehlgeschlagen.","Error al verificar el ajuste.","Falha ao verificar o ajuste."}},
            {"Restauration de protection echouee : fermez le jeu",new string[]{"Restoring protection failed: close the game","Restauration de protection echouee : fermez le jeu","Schutz konnte nicht wiederhergestellt werden: Spiel schließen","Error al restaurar la protección: cierra el juego","Falha ao restaurar a proteção: feche o jogo"}},
            {"Reglages hors limites.",new string[]{"Settings outside allowed limits.","Reglages hors limites.","Einstellungen außerhalb der zulässigen Grenzen.","Ajustes fuera de los límites permitidos.","Ajustes fora dos limites permitidos."}},
            {"Un autre changement de camera a ete detecte. Reglages suspendus.",new string[]{"Another camera change was detected. Settings suspended.","Un autre changement de camera a ete detecte. Reglages suspendus.","Andere Kameraänderung erkannt. Einstellungen ausgesetzt.","Se detectó otro cambio de cámara. Ajustes suspendidos.","Outra alteração na câmera foi detectada. Ajustes suspensos."}},
            {"Restauration partielle : un autre outil a change des valeurs.",new string[]{"Partial restore: another tool changed some values.","Restauration partielle : un autre outil a change des valeurs.","Teilweise wiederhergestellt: ein anderes Tool hat Werte geändert.","Restauración parcial: otra herramienta cambió algunos valores.","Restauração parcial: outra ferramenta alterou alguns valores."}},
        };
        internal static string Normalize(string code){return Array.IndexOf(Codes,code)>=0?code:"en";}
        internal static string Get(string key){
            if(key==null)return "";
            int index=Array.IndexOf(Codes,Language);if(index<0)index=0;
            string[] values;if(Texts.TryGetValue(key,out values))return values[index];
            foreach(var entry in Texts)if(key.StartsWith(entry.Key+" (Windows ",StringComparison.Ordinal))return entry.Value[index]+key.Substring(entry.Key.Length);
            return key;
        }
        internal static void SelfTest(){
            foreach(var entry in Texts){if(entry.Value.Length!=5)throw new Exception("Missing translation");foreach(string value in entry.Value)if(string.IsNullOrWhiteSpace(value))throw new Exception("Empty translation");}
            if(Normalize(null)!="en" || Normalize("unknown")!="en" || Normalize("pt-BR")!="pt-BR")throw new Exception("Language fallback failed");
            Language="de";if(!Get("ARC / BAGUETTE").Contains("ZAUBERSTAB"))throw new Exception("Language lookup failed");Language="en";
            if(Get("Lecture camera refusee (Windows 5)")!="Camera read denied (Windows 5)")throw new Exception("Error translation failed");
            Console.WriteLine("PASS: five complete language dictionaries, English fallback, runtime lookup and Windows error suffix.");
        }
    }
}
