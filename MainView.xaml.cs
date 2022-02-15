using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ZFinal_PlaceRoomTags
{
    /// <summary>
    /// Логика взаимодействия для MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public ExternalCommandData _commandData;
        public MainView(ExternalCommandData commandData)
        {
            InitializeComponent();

            _commandData = commandData;
            Document doc = _commandData.Application.ActiveUIDocument.Document;
            comboBoxAllPlanViews.ItemsSource = GetAllPlanViews(doc);
            comboBoxAllRoomTags.ItemsSource = GetAllRoomTags(doc);
        }

        private IEnumerable GetAllRoomTags(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_RoomTags)
                .OfType<FamilySymbol>()
                .ToList();
        }

        private List<View> GetAllPlanViews(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .WhereElementIsNotElementType()
                .OfType<View>()
                .Where(v=>((v.ViewType == ViewType.FloorPlan)||(v.ViewType == ViewType.CeilingPlan)))
                .Where(v=>v.IsTemplate==false)
                .OrderBy(vt=>vt.ViewType)
                .ToList();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Document doc = _commandData.Application.ActiveUIDocument.Document;
            List<Room> allRooms = new List<Room>();
            ElementId viewId = doc.ActiveView.Id;

            if (isSelectedView.IsChecked == true)
            {
                viewId = (comboBoxAllPlanViews.SelectedItem as View).Id;
            }

            allRooms = GetRoomsOnView(doc, viewId);
            if (allRooms == null || allRooms.Count == 0)
            {
                TaskDialog.Show("Error", $"В проекте не найдены помещения либо на текущем виде нельзя разместить марки помещений." +
                    $"\nДля размещения марок помещений создайте помещения или перейдите на подходящий вид.");
                this.Close();
            }

            // получаем марку помещений
            FamilySymbol roomTag = comboBoxAllRoomTags.SelectedItem as FamilySymbol;
            if (roomTag == null)
            {
                TaskDialog.Show("Info", $"Не выбрана марка помещений!");
                this.Focus();
                return;
            }

            // размещаем марки
            using (Transaction t = new Transaction(doc, "Разместить марки помещений"))
            {
                t.Start();

                try
                {
                    foreach (var room in allRooms)
                    {
                        CreateRoomTag(doc, room, roomTag, viewId);
                    }
                }
                catch (Exception ex)
                {

                    TaskDialog.Show("Error", ex.Message);
                }

                t.Commit();
                this.Focus();
            }
        }
        private List<Room> GetRoomsOnView(Document doc, ElementId viewId)
        {
            return new FilteredElementCollector(doc, viewId)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .Cast<Room>()
                .ToList();
        }

        private void CreateRoomTag(Document doc, Room room, FamilySymbol roomTag, ElementId viewId)
        {
            // получаем центр помещения
            XYZ roomCenterPoint = GetRoomCenter(room);
            Reference refRoom = new Reference(room);
            IndependentTag.Create(doc, roomTag.Id, viewId, refRoom, false, TagOrientation.Horizontal, roomCenterPoint);
        }

        private XYZ GetRoomCenter(Room room)
        {
            BoundingBoxXYZ boundingBox = room.get_BoundingBox(null);
            XYZ center = (boundingBox.Min + boundingBox.Max) * 0.5;
            XYZ centerXY = new XYZ(center.X, center.Y, 0);
            return centerXY;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
            return;
        }
    }

    //[ValueConversion(typeof(bool), typeof(bool))]
    //public class InverseBooleanConverter : IValueConverter
    //{
    //    #region IValueConverter Members
    //    public object Convert(object value, Type targetType, object parameter,
    //        System.Globalization.CultureInfo culture)
    //    {
    //        if (targetType != typeof(bool))
    //            throw new InvalidOperationException("The target must be a boolean");
    //        return !(bool)value;
    //    }
    //    public object ConvertBack(object value, Type targetType, object parameter,
    //        System.Globalization.CultureInfo culture)
    //    {
    //        throw new NotSupportedException();
    //    }
    //    #endregion
    //}
}
