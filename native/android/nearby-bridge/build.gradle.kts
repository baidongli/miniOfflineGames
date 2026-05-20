plugins {
    id("com.android.library")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "com.minigames.nearby"
    compileSdk = 34

    defaultConfig {
        minSdk = 23
        consumerProguardFiles("consumer-rules.pro")
    }

    buildTypes {
        release {
            isMinifyEnabled = false
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    kotlinOptions {
        jvmTarget = "17"
    }
}

dependencies {
    // Google Play Services Nearby SDK
    implementation("com.google.android.gms:play-services-nearby:19.3.0")
    // Provided by Unity at runtime - declared 'compileOnly' so it isn't bundled.
    compileOnly(files("libs/classes.jar")) // Unity classes.jar; drop in libs/ for local builds.
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.8.0")
    implementation("androidx.core:core-ktx:1.13.1")
}
