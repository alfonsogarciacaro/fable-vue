<template>
    <div class="draggable-header-view"
        @mousedown="startDrag" @touchstart="startDrag"
        @mousemove="onDrag" @touchmove="onDrag"
        @mouseup="stopDrag" @touchend="stopDrag" @mouseleave="stopDrag">
        <svg class="bg" width="320" height="560">
          <path :d="headerPath" fill="#3F51B5"></path>
        </svg>
        <div class="header">
          <slot name="header"></slot>
        </div>
        <div class="content" :style="contentPosition">
          <h1> {{title}} </h1>
          <slot name="content"></slot>
        </div>
    </div>
</template>

<script src="../src/DraggableHeader.fs"></script>

<style></style>

