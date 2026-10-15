# ميدان التحدي | Meydan Challenge

مسابقات عربية للفرق على أندرويد وويندوز: اختيار متعدد، أول ضغطة، إجابة جماعية، مؤقت، نقاط مضاعفة ولوحة نتائج.

**المدير يحدد عدد الأسئلة** من البنك ويضيف أو يستورد المزيد. الـ30 سؤالًا المرفقة محتوى تجريبي فقط. كل فريق لاعب أو أكثر، على جهاز واحد أو عدة أجهزة.

## التحميل والتجربة

- [صفحة التطبيق والتجربة](https://meydan-challenge.dewy-pin-3498.chatgpt.site/)
- [تنزيل APK مباشرة](https://github.com/alhasanelabed-sys/meydan-challenge/raw/refs/heads/main/downloads/meydan-android.apk)
- [تنزيل ويندوز المحمول مباشرة](https://github.com/alhasanelabed-sys/meydan-challenge/raw/refs/heads/main/downloads/meydan-windows-portable.zip)
- [الإصدارات وملفات التحميل](https://github.com/alhasanelabed-sys/meydan-challenge/releases)
- [دليل البداية](docs/guide-ar.md)
- [بنك الأسئلة التجريبي](shared/questions.json)

## الاتصال

- **بلوتوث في الإصدار الأول:** Bluetooth Classic RFCOMM، اقتران من إعدادات النظام، مدير أندرويد أو ويندوز.
- **شبكة محلية:** راوتر أو نقطة اتصال، دون إنترنت، منفذ TCP 45871 على جهاز المدير.
- **إنترنت:** خادم WebSocket منفصل عبر WSS. مصدره في `relay/`؛ يلزم إعداد استضافته. لا توجد خدمة عامة مستضافة جاهزة ضمن الإصدار.

يعرض التطبيق عدد الأجهزة والسعة التشغيلية المقدّرة وعدد الأجهزة الذي ثبت استقراره. يبدأ بمعايرة محافظة 4 للبلوتوث و32 للشبكة، ويمكن زيادة السعة وفق اختبار اتصال الأجهزة الفعلية واستقرارها. ليست هذه حدودًا مادية مضمونة ولا وعدًا بعدد اتصالات غير محدود.

## حالة الإصدار

**0.1.0 تجريبي.** بُني APK ونسخة ويندوز x64 المحمولة. اختبارات القواعد والرسائل والشبكة والواجهة تعمل في بيئة Linux، ومنها تشغيل نسخة Linux من المحرك نفسه ببيئة تشغيل .NET مضمّنة. لم يُجرَ اختبار فعلي للبلوتوث على هواتف أو ويندوز، ولا اختبار تشغيل ملف Windows EXE على ويندوز؛ نجاح البناء لا يثبت توافق العتاد.

تبقى واجهة المدير مفتوحة أثناء المسابقة، وعلى أندرويد يُفضل إبقاء التطبيق في الواجهة. أول ضغطة حسب وصولها إلى جهاز المدير، وقد يؤثر تفاوت زمن الاتصال في الترتيب.

## البناء

المتطلبات: Java JDK 21، Android SDK platform 35 وbuild-tools 35.0.0، Gradle 8.9، .NET SDK 8، Node.js 24+، Python 3.

```sh
npm ci --prefix relay
npm test
gradle -p android :app:assembleDebug
dotnet publish windows/Meydan.csproj -r win-x64 --self-contained true -c Release -p:PublishTrimmed=true -p:TrimMode=partial -o artifacts/windows
```

APK موقّع غير قابل للتصحيح: `bash scripts/build-android.sh` بعد ضبط `ANDROID_HOME`. لدعم تحديثات أندرويد بالمفتاح نفسه، اضبط `MEYDAN_KEYSTORE` و`MEYDAN_STORE_PASSWORD` على مفتاحك الثابت (alias: `meydan`). يحفظ البناء المحلي مفتاحه التجريبي في مجلد artifacts/.signing-private غير المتعقب. البناء التلقائي على عامل جديد يولّد مفتاحًا جديدًا؛ تثبيت بناء موقع بمفتاح مختلف يحتاج إزالة النسخة السابقة. لا تُرفع مفاتيح التوقيع إلى المستودع.

واجهة ويندوز تعمل في المتصفح الافتراضي عبر خادم loopback داخل البرنامج. لا يحتاج المستخدم إلى تثبيت .NET أو Node.js. تُرفق ملفات `assets/` مع EXE؛ احتفظ بها في مجلده.
