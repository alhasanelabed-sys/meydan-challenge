# ميدان التحدي Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** تسليم APK وحزمة ويندوز محمولة لمسابقة عربية متعددة الفرق مع البلوتوث والشبكة وإدارة السعة.

**Architecture:** محرك قواعد مستقل ورسائل JSON موحدة، ومحولات اتصال أصلية، وواجهة HTML/CSS/JavaScript محلية. أندرويد Java وWebView ومحولات RFCOMM وTCP؛ ويندوز .NET self-contained مع واجهة في المتصفح ومحولات TCP وRFCOMM. ترحيل الإنترنت منفصل عن التشغيل المحلي.

**Tech Stack:** Java 21، Android SDK/Gradle، C# .NET 8، JavaScript، بروتوكول JSON بإطارات سطرية وحد حجم 64 KiB.

**Spec:** /workspace/work/docs/spec.md

## Global Constraints
- أندرويد 10+، ويندوز 10/11 x64، العربية RTL.
- Bluetooth Classic RFCOMM في الإصدار الأول، UUID موحد 7591c0c6-2f53-4f19-a904-bef577040ba2.
- المدير يحدد عدد الأسئلة؛ 30 تخص العينة فقط.
- لا حد ثابت لعدد الفرق واللاعبين؛ السعة التشغيلية المقدرة للأجهزة منفصلة.
- القيم الأولية للمعايرة 4 بلوتوث و32 شبكة؛ نبض كل ثانيتين، نافذة 30 ثانية، فقد >10% أو p95 >1000 ms يوقف الانضمام؛ انقطاع بعد 10 ثوانٍ.
- لا خادم مستضاف أو نشر خارجي تلقائي. لا موارد خارجية للواجهة المحلية.

## Review Focus
1. إجابة صحيحة تسرّب إلى اللاعب قبل كشف المدير: تختبر إسقاط حقول الإجابة في snapshot.
2. لاعب يعيد الإرسال أو يغير توقيت هاتفه: تعتمد هوية الرسالة ووقت المدير فقط.
3. انقطاع فريق قبل الكشف: إعادة الهوية وحالتها دون احتساب نقاط مرتين.
4. صلاحيات البلوتوث مرفوضة أو محول غير موجود: رسالة واضحة واستمرار خيارات الشبكة.
5. بنك صغير أو إدخال عدد أسئلة غير صالح: رفض القيمة وتوضيح العدد المتاح، دون تثبيت 30.

### Task 1: قواعد المسابقة والواجهة المحلية
**Files:** shared/engine.js, shared/questions.json, shared/ui.js, shared/index.html, shared/style.css, tests/engine.test.mjs.
**Interfaces:** Engine.dispatch(memberId, message, nowMs): Result; Engine.snapshot(memberId): State; Engine.restore(savedState): Engine. الفريق والمشارك والجولة لديهم معرفات نصية؛ النتيجة لها accepted وreason وstateVersion.
- [ ] إنشاء اختبارات فاشلة باستخدام node --test: فريقان، عضوان للفريق، أول إجابة واحدة، الإقفال بعد المؤقت، رفض صلاحيات المدير، إخفاء الإجابة الصحيحة، استعادة الحالة ومنع احتساب مزدوج.
- [ ] تشغيل node --test tests/engine.test.mjs وتأكيد أن الفشل سببه غياب المحرك.
- [ ] تنفيذ المحرك والواجهة: إعداد عدد الأسئلة، بنك ومحرر واستيراد وتصدير، مؤقت ونتائج ومحاكاة أربعة فرق، حفظ محلي.
- [ ] اختبار اختيار 1 و30 و31 سؤالًا من بنك 30، مع إضافة سؤال ليصبح 31 صالحًا. اختبار أسماء تحتوي HTML وعرضها كنص.
- [ ] تشغيل اختبارات المحرك وفحص الواجهة محليًا بأحجام هاتف وشاشة كبيرة.

### Task 2: الاتصال والمعايرة
**Files:** shared/protocol.js, shared/capacity.js, tests/protocol.test.mjs, tests/capacity.test.mjs.
**Interfaces:** encode(message): string; decode(line): Message; Capacity.observe(peerId, rttMs, receivedAtMs): void; Capacity.canAdmit(nowMs): boolean; Capacity.report(nowMs): Report.
- [ ] اختبارات فاشلة لحجم أكبر من 64 KiB، جولة قديمة، رسالة مكررة، استقرار 4 أجهزة ثم إضافة خامس، فقد >10%، p95 >1000 ms، وإعادة الاتصال دون هوية جديدة.
- [ ] تشغيل الاختبارات وتأكيد أسباب الفشل، ثم تنفيذ الترميز وسياسة السعة وفق المواصفات.
- [ ] تشغيل node --test tests/*.test.mjs والتحقق من عدم مساواة عدد أجهزة قليل بالحد المادي.

### Task 3: أندرويد والاستضافة والانضمام
**Files:** android/settings.gradle, android/build.gradle, android/app/build.gradle, android/app/src/main/AndroidManifest.xml, android/app/src/main/java/app/meydan/MainActivity.java, BluetoothTransport.java, TcpTransport.java, AssetServer.java.
**Interfaces:** Transport.startHost(listener): void; Transport.connect(address, listener): void; Transport.send(peerId, line): void; Transport.close(): void. رسائل التطبيق تمر بنفس التحقق في جميع المحولات.
- [ ] تجهيز SDK/Gradle داخل work/tools، وإنشاء اختبار JVM للإطارات والانقطاع والحجم قبل المحولات.
- [ ] تنفيذ BluetoothAdapter وBluetoothServerSocket وBluetoothSocket بخدمة UUID المذكورة، وصلاحيات CONNECT/SCAN حسب إصدار النظام، مع معالجة الرفض وعدم التوفر.
- [ ] تنفيذ الاستضافة الشبكية والانضمام، وربط الواجهة المحلية عبر WebView مع قصر الجسر الأصلي على محتوى التطبيق.
- [ ] تشغيل اختبارات Java ثم gradle :app:assembleDebug، والتحقق من APK بواسطة apksigner وaapt.
- [ ] تسجيل اختبار البلوتوث على هاتفين كاختبار عتادي مطلوب إذا لم تتوفر الأجهزة، دون الادعاء بأنه اجتاز.

### Task 4: ويندوز المحمول وتوافق الرسائل
**Files:** windows/Meydan.csproj, windows/Program.cs, windows/BluetoothTransport.cs, windows/TcpTransport.cs, windows/AssetServer.cs, tests/interop.test.mjs.
**Interfaces:** نفس Transport ومعرف الخدمة، JSON UTF-8، إطار ينتهي بسطر جديد. خادم الواجهة مقصور على loopback؛ منفذ اتصال اللاعبين منفصل ويطلب رمز جلسة.
- [ ] اختبار توافق الرسائل بين Java وC# بقيم عربية، وفصل وإعادة اتصال، وحجم زائد، قبل تنفيذ المحولات.
- [ ] تجهيز .NET SDK محليًا، وتنفيذ محرك ويندوز وخادم الواجهة وفتح المتصفح، مع RFCOMM عبر مكتبة موثوقة متوافقة أو WinSock أصلي.
- [ ] تشغيل اختبارات الرسائل المحلية والقواعد؛ نشر dotnet publish -r win-x64 --self-contained true -c Release.
- [ ] التحقق من ملفات PE والأصول وعدم الاعتماد على .NET مثبت، وتسجيل اختبار تشغيل ويندوز وبلوتوث هاتف/ويندوز إذا تعذر عتاديًا.

### Task 5: الإنترنت والحزمة النهائية
**Files:** relay/server.mjs, relay/package.json, tests/relay.test.mjs, deliverables/README-ar.md, scripts/package.py.
**Interfaces:** join(roomCode, token, role) ومغلفات protocol نفسها؛ مدير واحد، جلسة معزولة، انتهاء صلاحية الجلسة عند انقطاع المدير المطول.
- [ ] اختبارات فاشلة لعزل الغرف، رمز غير صحيح، انتحال المدير، إعادة الاتصال، وحد حجم الرسائل.
- [ ] تنفيذ ترحيل WebSocket وتعليمات TLS وتشغيله، وربط خيار الإنترنت بالعميل دون نشره خارجيًا.
- [ ] تشغيل اختبارات التكامل بين مضيف وعميلين حقيقيين عبر localhost، ومراجعة منع تسريب الإجابات وصلاحيات الإدارة.
- [ ] بناء APK وحزمة ويندوز من الحالة النهائية، تضمين العينة والدليل والمصادر، توليد SHA-256، وتوثيق الاختبارات الفعلية وغير المنفذة بدقة.

## Handoff
تُراجع الخطة قبل التنفيذ. التنفيذ المقترح مباشرة داخل هذه المحادثة؛ لا تفويض لوكلاء دون اختيار المستخدم. لا التزام بدعم عتادي مثبت دون اختبارات الأجهزة، ولا تسليم وهمي لملفات تعذر بناؤها.
