# MotionLink grip-butt enclosure

HW-02 fit prototype for the Callaway grip shown in the photos. The enclosure is 34 × 30.5 × 16.2 mm before the mount, with an external USB-C opening and a side opening for the prewired slide switch.

## Print the fit parts

The 3.4 mm [vent gauge](vent-gauges.stl) was the best hand fit on the tested grip. Confirm the fit on each club you want to use. The five gauges range from 2.6 to 3.4 mm and have a 4 mm pin.

1. Print the [3.4 mm smooth mount](mount-smooth-3.4mm.stl) to try the same pin profile with the enclosure's dished seat. Check that the seat reaches the grip cap and sits without rocking.
2. For the screw-in HW-02 check, print the [3.6 mm shallow-thread mount](mount-threaded-3.6mm.stl). Its 3.3 mm core is slightly smaller than the tested gauge and its rounded crests reach 3.6 mm. Try it by hand with light pressure and stop if it binds, marks the grip, or fails to seat.

Use the Ender-3 V3 Plus PLA profile with the stock 0.4 mm nozzle. Start at 0.12 mm layer height and solid infill for the mounts. Print the mount with the broad face down and the plug up. Leave the nut recess clear of supports. The tray and lid can also print in the supplied orientations, at 0.16–0.20 mm layer height.

## Assemble the draft housing

1. Print [tray.stl](tray.stl) and [lid.stl](lid.stl). Fit an M3 hex nut into the back of the mount, then fasten the mount with an M3 × 5 mm button-head screw from inside the empty tray. Check that the screw head sits below the tray floor and that the screw tightens without bottoming out. Cover the recess with a thin insulating layer before installing the electronics.
2. Drop the taped assembly in with the pouch cell down, USB-C at the broad front notch and the metal switch body on its side shelf. Check wire clearance, switch travel, and that your USB-C cable can seat fully. The board and switch must stay fixed for repeatable IMU readings. If needed, use a thin, removable pad against the PCB shield to take up play. If the lid presses on the LiPo pouch, pinches a wire, or requires force to close, adjust the CAD.
3. Flip the lid over so its raised tab is inside over the switch. Fasten it with four M2 × 6 mm screws into the printed pilot holes, taking care not to strip them.

## Housing for the current soldered assembly

Use [tray-wired.stl](tray-wired.stl) with [lid-wired.stl](lid-wired.stl). This revision extends the tray 5 mm at the USB-C end so the battery leads can sit in front of the board. The tray rim is 14.4 mm high, matching the original tray. The USB-C opening is wider to accommodate the cable plug. The switch shelf and actuator position match the first print. Transfer the tested grip mount using the same M3 fastener.

Lay the battery leads in the space in front of the board and below the USB-C connector. Check that the assembly sits without rocking, the connector stays clear, and the lid rests flat without pressing on the LiPo or trapping the leads. Test your USB-C cable and the full switch travel with the lid in place.

The revised lid uses two M3 × 6–10 mm pan- or button-head screws with open pilot holes in the tray. Start the screws by hand in the empty tray and stop once snug. Tape can hold the lid for a bench fit check. Fasten the lid securely for swing testing.

The revised housing measures 34 × 35.5 × 16.6 mm before the mount. The original tray and lid are also available above if the wiring is reworked.

## Verify HW-02

The first housing mounted cleanly on the wedge and 6-iron in the initial fit check. Check the revised enclosure empty first, then with the electronics installed. On each club, check that the case seats without rocking, resists firm hand pull and twist, and leaves no marks after removal. Repeat after several fit/removal cycles. Progress to slow practice swings in a clear area and recheck for rotation, looseness or a cracked plug. Any movement needs a mount revision before full swings. Keep the case aligned consistently relative to the clubface when transferring it between clubs. Record the threaded mount's result against the screw-in requirement.

The supplied meshes are prototypes. Final fit depends on the measured vent and cap shape, print tolerances, and the actual dimensions of the taped assembly. If a case fit needs adjusting, measure the stack height including solder and wires, the battery envelope with tape, and the switch face location relative to the USB-C connector. [motionlink.scad](motionlink.scad) exposes the mount diameters and length, seat diameter and dish, case bounds, and opening positions. Choose a part in OpenSCAD, render with F6, and export as STL.

The battery and switch dimensions came from the supplier listing and drawing. The positions of the wired components were estimated from the photos. [LiPo](https://thepihut.com/products/150mah-3-7v-lipo-battery-1-25mm-picoblade-connector?variant=55102987567489) · [Switch drawing](https://cdn.shopify.com/s/files/1/0176/3274/files/Datasheet.jpg?v=1692628891)
